using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoombaNet.Proxy.Configuration;

namespace RoombaNet.Proxy.Brokers;

/// <summary>
/// The native, drop-in TLS listener. Clients connect exactly as they would to a real robot
/// (TLS on 8883, username = client-id = BLID, password = the robot's local password) and see that
/// robot's native topics. Routing, authentication and isolation are all keyed off the authenticated BLID.
/// </summary>
public sealed class NativeDropInBroker : IAsyncDisposable
{
    private const string BlidSessionKey = "roomba-blid";

    private readonly ILogger _logger;
    private readonly ProxySettings _settings;
    private readonly IReadOnlyDictionary<string, ProxyRobotDescriptor> _robots;
    private readonly RelayToRobot _relay;
    private readonly X509Certificate2 _certificate;
    private readonly MqttServer _server;
    private readonly ConcurrentDictionary<string, string> _clientBlid = new();

    /// <summary>Relays an allow-listed client message to the robot's upstream connection.</summary>
    public delegate Task RelayToRobot(string blid, string topic, byte[] payload, MqttQualityOfServiceLevel qos, CancellationToken cancellationToken);

    public NativeDropInBroker(
        ILogger logger,
        ProxySettings settings,
        IReadOnlyDictionary<string, ProxyRobotDescriptor> robots,
        RelayToRobot relay,
        X509Certificate2 certificate)
    {
        _logger = logger;
        _settings = settings;
        _robots = robots;
        _relay = relay;
        _certificate = certificate;

        var options = new MqttServerFactory().CreateServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(settings.Tls.Port)
            .WithEncryptionCertificate(_certificate)
            .Build();

        _server = new MqttServerFactory().CreateMqttServer(options);
        _server.ValidatingConnectionAsync += OnValidatingConnectionAsync;
        _server.ClientDisconnectedAsync += OnClientDisconnectedAsync;
        _server.InterceptingClientEnqueueAsync += OnInterceptingEnqueueAsync;
        _server.InterceptingPublishAsync += OnInterceptingPublishAsync;
    }

    /// <summary>Starts the TLS listener.</summary>
    public async Task StartAsync()
    {
        await _server.StartAsync();
        _logger.LogInformation("Native drop-in TLS listener started on port {Port}", _settings.Tls.Port);
    }

    /// <summary>Injects a live robot message onto the broker under its native topic.</summary>
    public Task InjectFromRobotAsync(string blid, string topic, byte[] payload, CancellationToken cancellationToken)
        => InjectAsync(blid, topic, payload, retain: false, cancellationToken);

    /// <summary>Publishes the merged shadow snapshot (retained) so late-joining clients get the full state.</summary>
    public async Task PublishSnapshotAsync(string blid, byte[] shadowDocument, CancellationToken cancellationToken)
    {
        await InjectAsync(blid, ProxyTopics.ShadowUpdateTopic(blid), shadowDocument, retain: true, cancellationToken);
    }

    /// <summary>Publishes the retained online/offline status for a robot.</summary>
    public async Task SetStatusAsync(string blid, bool online, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(online ? "online" : "offline");
        await InjectAsync(blid, ProxyTopics.StatusTopic(blid), payload, retain: true, cancellationToken);
    }

    private async Task InjectAsync(string blid, string topic, byte[] payload, bool retain, CancellationToken cancellationToken)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var injected = new InjectedMqttApplicationMessage(message)
        {
            SenderClientId = ProxyTopics.UpstreamSender(blid),
        };

        await _server.InjectApplicationMessage(injected, cancellationToken);
    }

    private Task OnValidatingConnectionAsync(ValidatingConnectionEventArgs args)
    {
        if (!args.IsSecureConnection)
        {
            _logger.LogWarning("Rejecting non-TLS connection {ClientId} on the native listener", args.ClientId);
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return Task.CompletedTask;
        }

        var username = args.UserName ?? string.Empty;

        if (!_robots.TryGetValue(username, out var robot) || !string.Equals(robot.Password, args.Password, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejecting client {ClientId}: unknown or mismatched credentials for '{UserName}'", args.ClientId, username);
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return Task.CompletedTask;
        }

        // Drop-in clients use client-id == BLID; multiplex-friendly clients may use "<blid>-<suffix>"
        // to avoid session takeover. Routing is always by the authenticated username (BLID).
        var clientId = args.ClientId ?? string.Empty;
        if (!string.Equals(clientId, username, StringComparison.Ordinal)
            && !clientId.StartsWith($"{username}-", StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejecting client {ClientId}: client-id must equal or be prefixed by the BLID '{UserName}'", clientId, username);
            args.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            return Task.CompletedTask;
        }

        args.SessionItems[BlidSessionKey] = robot.Blid;
        _clientBlid[clientId] = robot.Blid;
        args.ReasonCode = MqttConnectReasonCode.Success;
        _logger.LogInformation("Client {ClientId} authenticated for robot {Blid}", clientId, robot.Blid);
        return Task.CompletedTask;
    }

    private Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs args)
    {
        _clientBlid.TryRemove(args.ClientId, out _);
        return Task.CompletedTask;
    }

    private Task OnInterceptingEnqueueAsync(InterceptingClientApplicationMessageEnqueueEventArgs args)
    {
        var senderBlid = ProxyTopics.TryGetUpstreamBlid(args.SenderClientId)
                         ?? (_clientBlid.TryGetValue(args.SenderClientId ?? string.Empty, out var sb) ? sb : null);

        var receiverBlid = _clientBlid.TryGetValue(args.ReceiverClientId, out var rb) ? rb : null;

        // Enforce isolation: a client only ever receives messages belonging to its own robot.
        if (senderBlid is not null && receiverBlid is not null && !string.Equals(senderBlid, receiverBlid, StringComparison.Ordinal))
        {
            args.AcceptEnqueue = false;
        }

        return Task.CompletedTask;
    }

    private async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        // Ignore messages the proxy itself injected on behalf of a robot (avoids relay loops).
        if (ProxyTopics.TryGetUpstreamBlid(args.ClientId) is not null)
        {
            return;
        }

        if (args.SessionItems[BlidSessionKey] is not string blid)
        {
            return;
        }

        var topic = args.ApplicationMessage.Topic;
        if (!ProxyTopics.IsRobotBound(topic, blid))
        {
            return;
        }

        var payload = args.ApplicationMessage.Payload.ToArray();
        await _relay(blid, topic, payload, args.ApplicationMessage.QualityOfServiceLevel, args.CancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _server.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping native broker");
        }

        _server.Dispose();
        _certificate.Dispose();
    }
}
