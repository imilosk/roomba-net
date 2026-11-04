using System.Buffers;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using RoombaNet.Proxy.Configuration;
using RoombaNet.Proxy.State;

namespace RoombaNet.Proxy.Upstream;

/// <summary>
/// Holds the single real MQTT connection to one robot and multiplexes it to the proxy brokers.
/// Models RoombaNet.Transport.Mqtt.RoombaConnectionManager for the upstream options and adds
/// auto-reconnect with backoff, full-shadow re-subscription on every (re)connect, and a merged state view.
/// </summary>
public sealed class UpstreamRobotConnection : IAsyncDisposable
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly ILogger _logger;
    private readonly ProxyRobotDescriptor _robot;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly MqttClientFactory _factory;
    private readonly CancellationTokenSource _stopCts = new();

    private TaskCompletionSource _disconnectedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _runLoop;

    /// <summary>Raised for every message received from the robot, after the merged state has been updated.</summary>
    public Func<string, byte[], CancellationToken, Task>? MessageReceived { get; set; }

    /// <summary>Raised whenever the upstream connection transitions online/offline.</summary>
    public Func<bool, CancellationToken, Task>? ConnectionStateChanged { get; set; }

    public UpstreamRobotConnection(ILogger logger, MqttClientFactory factory, ProxyRobotDescriptor robot)
    {
        _logger = logger;
        _factory = factory;
        _robot = robot;
        _client = factory.CreateMqttClient();
        _options = BuildOptions(robot);

        _client.ApplicationMessageReceivedAsync += OnMessageAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    /// <summary>Robot BLID this connection serves.</summary>
    public string Blid => _robot.Blid;

    /// <summary>Deep-merged <c>state.reported</c> snapshot for this robot.</summary>
    public MergedShadowState State { get; } = new();

    /// <summary>Whether the upstream connection is currently established.</summary>
    public bool IsConnected => _client.IsConnected;

    /// <summary>Starts the background connect/reconnect loop.</summary>
    public void Start()
    {
        _runLoop ??= Task.Run(() => RunAsync(_stopCts.Token));
    }

    /// <summary>Relays a client message to the robot. No-op when the upstream is not connected.</summary>
    public async Task PublishToRobotAsync(string topic, byte[] payload, MqttQualityOfServiceLevel qos, CancellationToken cancellationToken)
    {
        if (!_client.IsConnected)
        {
            _logger.LogWarning("[{Blid}] Dropping relay to '{Topic}': upstream not connected", _robot.Blid, topic);
            return;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
        _logger.LogDebug("[{Blid}] client -> robot {Topic} ({Bytes} bytes)", _robot.Blid, topic, payload.Length);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var backoff = MinBackoff;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _disconnectedSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                _logger.LogInformation("[{Blid}] Connecting upstream to {Ip}:{Port} (tls={Tls})",
                    _robot.Blid, _robot.Ip, _robot.Port, _robot.UseTls);

                var result = await _client.ConnectAsync(_options, cancellationToken);

                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    backoff = MinBackoff;
                    await OnConnectedAsync(cancellationToken);

                    // Wait until the connection drops, then loop to reconnect.
                    await _disconnectedSignal.Task.WaitAsync(cancellationToken);
                    await RaiseStateChangedAsync(false, cancellationToken);
                }
                else
                {
                    _logger.LogError("[{Blid}] Upstream connect failed: {Code} {Reason}",
                        _robot.Blid, result.ResultCode, result.ReasonString);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Blid}] Upstream connection error", _robot.Blid);
            }

            try
            {
                await Task.Delay(backoff, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, MaxBackoff.TotalMilliseconds));
        }
    }

    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Blid}] Upstream connected", _robot.Blid);

        var shadowRoot = ProxyTopics.ShadowRoot(_robot.Blid);

        // "#" does not deliver reserved $-topics, so subscribe to the shadow tree explicitly as well.
        var subscribeOptions = _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(ProxyTopics.All).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic($"{shadowRoot}/#").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
        await _client.SubscribeAsync(subscribeOptions, cancellationToken);

        // Ask for the full shadow after every (re)connect; harmless no-op on firmware that ignores it.
        var shadowGet = new MqttApplicationMessageBuilder()
            .WithTopic(ProxyTopics.ShadowGetTopic(_robot.Blid))
            .WithPayload(Array.Empty<byte>())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();
        await _client.PublishAsync(shadowGet, cancellationToken);

        await RaiseStateChangedAsync(true, cancellationToken);
    }

    private async Task OnMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.Payload.ToArray();

        State.TryMerge(payload);

        var handler = MessageReceived;
        if (handler is not null)
        {
            try
            {
                await handler(topic, payload, _stopCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Blid}] Error dispatching upstream message on '{Topic}'", _robot.Blid, topic);
            }
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning("[{Blid}] Upstream disconnected: {Reason}", _robot.Blid, e.Reason);
        _disconnectedSignal.TrySetResult();
        return Task.CompletedTask;
    }

    private async Task RaiseStateChangedAsync(bool online, CancellationToken cancellationToken)
    {
        var handler = ConnectionStateChanged;
        if (handler is not null)
        {
            try
            {
                await handler(online, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Blid}] Error dispatching connection state change", _robot.Blid);
            }
        }
    }

    private static MqttClientOptions BuildOptions(ProxyRobotDescriptor robot)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(robot.Ip, robot.Port)
            .WithCredentials(robot.Blid, robot.Password)
            .WithCleanSession()
            .WithProtocolVersion(MqttProtocolVersion.V311);

        if (robot.UseTls)
        {
            // The robot requires the client-id to equal its BLID for the TLS connection.
            builder = builder
                .WithClientId(robot.Blid)
                .WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithCertificateValidationHandler(_ => true);
                });
        }
        else
        {
            // Plain/proxy mode: a non-BLID client-id is used so this connection does not collide with
            // a strict drop-in client that uses the BLID as its client-id.
            builder = builder.WithClientId($"{robot.Blid}-{Guid.NewGuid():N}");
        }

        return builder.Build();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stopCts.CancelAsync();
        _disconnectedSignal.TrySetResult();

        if (_runLoop is not null)
        {
            try
            {
                await _runLoop;
            }
            catch (OperationCanceledException)
            {
                // expected during shutdown
            }
        }

        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[{Blid}] Error during upstream disconnect", _robot.Blid);
        }

        _client.Dispose();
        _stopCts.Dispose();
    }
}
