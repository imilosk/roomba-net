using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Proxy.Brokers;
using RoombaNet.Proxy.Configuration;
using RoombaNet.Proxy.Security;
using RoombaNet.Proxy.Upstream;

namespace RoombaNet.Proxy;

/// <summary>
/// Orchestrates the multi-robot MQTT proxy: one upstream connection per robot multiplexed onto an
/// embedded native (drop-in TLS) broker and an optional aggregated plain broker.
/// </summary>
public sealed class RoombaProxyService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<RoombaProxyService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ProxySettings _settings;
    private readonly IProxyRobotSource _robotSource;
    private readonly MqttClientFactory _mqttClientFactory;

    private readonly List<UpstreamRobotConnection> _upstreams = [];
    private readonly Dictionary<string, UpstreamRobotConnection> _upstreamByBlid = new(StringComparer.Ordinal);

    private NativeDropInBroker? _nativeBroker;
    private AggregatedPlainBroker? _plainBroker;
    private bool _started;

    public RoombaProxyService(
        ILogger<RoombaProxyService> logger,
        ILoggerFactory loggerFactory,
        ProxySettings settings,
        IProxyRobotSource robotSource,
        MqttClientFactory mqttClientFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _settings = settings;
        _robotSource = robotSource;
        _mqttClientFactory = mqttClientFactory;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        var robots = await _robotSource.GetRobotsAsync(cancellationToken);
        if (robots.Count == 0)
        {
            _logger.LogWarning("Proxy enabled but no robots were resolved; nothing to proxy.");
            return;
        }

        var robotMap = robots.ToDictionary(r => r.Blid, r => r, StringComparer.Ordinal);

        var certificate = SelfSignedCertificateFactory.CreateOrLoad(
            _settings.Tls.CertificatePath,
            _settings.Tls.CertificatePassword,
            _logger);

        _nativeBroker = new NativeDropInBroker(
            _loggerFactory.CreateLogger<NativeDropInBroker>(),
            _settings,
            robotMap,
            RelayToRobotAsync,
            certificate);
        await _nativeBroker.StartAsync();

        if (_settings.Plain.Enabled)
        {
            _plainBroker = new AggregatedPlainBroker(
                _loggerFactory.CreateLogger<AggregatedPlainBroker>(),
                _settings,
                RelayToRobotAsync);
            await _plainBroker.StartAsync();
        }

        foreach (var robot in robots)
        {
            var upstream = new UpstreamRobotConnection(
                _loggerFactory.CreateLogger<UpstreamRobotConnection>(),
                _mqttClientFactory,
                robot);

            upstream.MessageReceived = (topic, payload, ct) => OnRobotMessageAsync(robot.Blid, topic, payload, ct);
            upstream.ConnectionStateChanged = (online, ct) => OnRobotStateAsync(robot.Blid, online, ct);

            _upstreams.Add(upstream);
            _upstreamByBlid[robot.Blid] = upstream;
        }

        foreach (var upstream in _upstreams)
        {
            upstream.Start();
        }

        _started = true;
        _logger.LogInformation("RoombaNet proxy started for {Count} robot(s): {Blids}",
            robots.Count, string.Join(", ", robots.Select(r => r.Blid)));
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
    }

    private async Task OnRobotMessageAsync(string blid, string topic, byte[] payload, CancellationToken cancellationToken)
    {
        if (_nativeBroker is not null)
        {
            await _nativeBroker.InjectFromRobotAsync(blid, topic, payload, cancellationToken);
        }

        if (_plainBroker is not null && _settings.Plain.Aggregated)
        {
            await _plainBroker.InjectFromRobotAsync(blid, topic, payload, cancellationToken);
        }

        await PublishSnapshotsAsync(blid, cancellationToken);
    }

    private async Task OnRobotStateAsync(string blid, bool online, CancellationToken cancellationToken)
    {
        if (_nativeBroker is not null)
        {
            await _nativeBroker.SetStatusAsync(blid, online, cancellationToken);
        }

        if (_plainBroker is not null && _settings.Plain.Aggregated)
        {
            await _plainBroker.SetStatusAsync(blid, online, cancellationToken);
        }

        if (online)
        {
            await PublishSnapshotsAsync(blid, cancellationToken);
        }
    }

    private async Task PublishSnapshotsAsync(string blid, CancellationToken cancellationToken)
    {
        if (!_upstreamByBlid.TryGetValue(blid, out var upstream) || !upstream.State.HasState)
        {
            return;
        }

        if (_nativeBroker is not null)
        {
            var shadow = upstream.State.SnapshotShadowDocument();
            if (shadow is not null)
            {
                await _nativeBroker.PublishSnapshotAsync(blid, shadow, cancellationToken);
            }
        }

        if (_plainBroker is not null && _settings.Plain.Aggregated)
        {
            var reported = upstream.State.SnapshotReported();
            if (reported is not null)
            {
                await _plainBroker.PublishSnapshotAsync(blid, reported, cancellationToken);
            }
        }
    }

    private Task RelayToRobotAsync(string blid, string topic, byte[] payload, MqttQualityOfServiceLevel qos, CancellationToken cancellationToken)
    {
        if (_upstreamByBlid.TryGetValue(blid, out var upstream))
        {
            return upstream.PublishToRobotAsync(topic, payload, qos, cancellationToken);
        }

        _logger.LogWarning("No upstream for '{Blid}'; dropping relayed '{Topic}'", blid, topic);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_started)
        {
            return;
        }

        _started = false;

        foreach (var upstream in _upstreams)
        {
            await upstream.DisposeAsync();
        }

        _upstreams.Clear();
        _upstreamByBlid.Clear();

        if (_plainBroker is not null)
        {
            await _plainBroker.DisposeAsync();
            _plainBroker = null;
        }

        if (_nativeBroker is not null)
        {
            await _nativeBroker.DisposeAsync();
            _nativeBroker = null;
        }
    }
}
