using System.Buffers;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoombaNet.Proxy.Configuration;

namespace RoombaNet.Proxy.Brokers;

/// <summary>
/// Convenience plain (no-TLS) listener bound to localhost for quick tooling (mosquitto_sub, scripts,
/// Home Assistant). When aggregation is enabled every robot is mirrored under
/// <c>roomba/&lt;blid&gt;/&lt;topic&gt;</c> with a merged retained snapshot on <c>roomba/&lt;blid&gt;/state</c>,
/// so a single subscription can watch every robot. No authentication is performed on this listener.
/// </summary>
public sealed class AggregatedPlainBroker : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ProxySettings _settings;
    private readonly NativeDropInBroker.RelayToRobot _relay;
    private readonly MqttServer _server;

    public AggregatedPlainBroker(
        ILogger logger,
        ProxySettings settings,
        NativeDropInBroker.RelayToRobot relay)
    {
        _logger = logger;
        _settings = settings;
        _relay = relay;

        var options = new MqttServerFactory().CreateServerOptionsBuilder()
            .WithoutEncryptedEndpoint()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(settings.Plain.Port)
            .WithDefaultEndpointBoundIPAddress(ResolveHost(settings.Plain.Host))
            .Build();

        _server = new MqttServerFactory().CreateMqttServer(options);
        _server.InterceptingPublishAsync += OnInterceptingPublishAsync;
    }

    /// <summary>Starts the plain listener.</summary>
    public async Task StartAsync()
    {
        await _server.StartAsync();
        _logger.LogInformation("Plain listener started on {Host}:{Port} (aggregated={Aggregated})",
            _settings.Plain.Host, _settings.Plain.Port, _settings.Plain.Aggregated);
    }

    /// <summary>Mirrors a live robot message onto the aggregated view.</summary>
    public Task InjectFromRobotAsync(string blid, string topic, byte[] payload, CancellationToken cancellationToken)
        => InjectAsync(ViewTopic(blid, topic), payload, retain: false, cancellationToken);

    /// <summary>Publishes the merged reported snapshot (retained) on <c>roomba/&lt;blid&gt;/state</c>.</summary>
    public Task PublishSnapshotAsync(string blid, byte[] reportedSnapshot, CancellationToken cancellationToken)
        => InjectAsync($"{ProxyTopics.AggregatedPrefix}/{blid}/state", reportedSnapshot, retain: true, cancellationToken);

    /// <summary>Publishes the retained online/offline status on <c>roomba/&lt;blid&gt;/status</c>.</summary>
    public Task SetStatusAsync(string blid, bool online, CancellationToken cancellationToken)
        => InjectAsync($"{ProxyTopics.AggregatedPrefix}/{blid}/status", Encoding.UTF8.GetBytes(online ? "online" : "offline"), retain: true, cancellationToken);

    private string ViewTopic(string blid, string topic) => $"{ProxyTopics.AggregatedPrefix}/{blid}/{topic}";

    private async Task InjectAsync(string topic, byte[] payload, bool retain, CancellationToken cancellationToken)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var injected = new InjectedMqttApplicationMessage(message)
        {
            SenderClientId = ProxyTopics.UpstreamSender("plain"),
        };

        await _server.InjectApplicationMessage(injected, cancellationToken);
    }

    private async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        // Skip messages the proxy injected itself to avoid relay loops.
        if (ProxyTopics.TryGetUpstreamBlid(args.ClientId) is not null)
        {
            return;
        }

        // Convenience relay: clients may publish roomba/<blid>/<allowlisted-topic> to command a robot.
        var parts = args.ApplicationMessage.Topic.Split('/', 3);
        if (parts.Length < 3 || !string.Equals(parts[0], ProxyTopics.AggregatedPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var blid = parts[1];
        var robotTopic = parts[2];
        if (!ProxyTopics.IsRobotBound(robotTopic, blid))
        {
            return;
        }

        var payload = args.ApplicationMessage.Payload.ToArray();
        await _relay(blid, robotTopic, payload, args.ApplicationMessage.QualityOfServiceLevel, args.CancellationToken);
    }

    private static IPAddress ResolveHost(string host)
        => IPAddress.TryParse(host, out var address) ? address : IPAddress.Loopback;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _server.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping plain broker");
        }

        _server.Dispose();
    }
}
