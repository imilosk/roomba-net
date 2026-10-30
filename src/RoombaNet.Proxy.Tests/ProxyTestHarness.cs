using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using RoombaNet.Proxy;
using RoombaNet.Proxy.Configuration;

namespace RoombaNet.Proxy.Tests;

/// <summary>
/// Spins up a running proxy backed by one in-memory MQTT broker per fake robot, so tests exercise the
/// real brokers and relay paths without touching a real robot.
/// </summary>
public sealed class ProxyTestHarness : IAsyncDisposable
{
    private readonly List<FakeRobot> _robots = [];
    private readonly List<IMqttClient> _clients = [];
    private readonly MqttClientFactory _clientFactory = new();
    private RoombaProxyService? _proxy;

    public int TlsPort { get; private set; }
    public int PlainPort { get; private set; }

    public static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Registers a fake robot and starts its in-memory broker. Call before <see cref="StartAsync"/>.</summary>
    public async Task<FakeRobot> AddRobotAsync(string blid, string password)
    {
        var robot = new FakeRobot(blid, password, FreePort());
        await robot.StartAsync();
        _robots.Add(robot);
        return robot;
    }

    public async Task StartAsync(bool enablePlain = false, bool aggregated = true)
    {
        TlsPort = FreePort();
        PlainPort = FreePort();

        var settings = new ProxySettings
        {
            Enabled = true,
            Tls = new TlsListenerSettings { Port = TlsPort },
            Plain = new PlainListenerSettings { Enabled = enablePlain, Host = "127.0.0.1", Port = PlainPort, Aggregated = aggregated },
        };

        var descriptors = _robots
            .Select(r => new ProxyRobotDescriptor { Blid = r.Blid, Ip = "127.0.0.1", Port = r.Port, Password = r.Password, UseTls = false })
            .ToList();

        _proxy = new RoombaProxyService(
            NullLogger<RoombaProxyService>.Instance,
            NullLoggerFactory.Instance,
            settings,
            new StaticRobotSource(descriptors),
            _clientFactory);

        await _proxy.StartAsync(CancellationToken.None);

        // Allow the upstream connections to establish.
        await WaitUntilAsync(() => _robots.All(r => r.HasUpstream), TimeSpan.FromSeconds(10));
    }

    /// <summary>Connects a drop-in client to the native TLS listener.</summary>
    public async Task<CollectingClient> ConnectNativeAsync(string blid, string password, string? clientId = null)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", TlsPort)
            .WithClientId(clientId ?? blid)
            .WithCredentials(blid, password)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithCleanSession()
            .WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(_ => true);
            })
            .Build();

        return await ConnectAsync(options);
    }

    /// <summary>Connects a client to the plain localhost listener.</summary>
    public async Task<CollectingClient> ConnectPlainAsync(string clientId)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", PlainPort)
            .WithClientId(clientId)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithCleanSession()
            .Build();

        return await ConnectAsync(options);
    }

    private async Task<CollectingClient> ConnectAsync(MqttClientOptions options)
    {
        var client = _clientFactory.CreateMqttClient();
        var collector = new CollectingClient(client);
        _clients.Add(client);
        var result = await client.ConnectAsync(options, CancellationToken.None);
        if (result.ResultCode != MqttClientConnectResultCode.Success)
        {
            throw new InvalidOperationException($"Connect failed: {result.ResultCode} {result.ReasonString}");
        }

        return collector;
    }

    public FakeRobot Robot(string blid) => _robots.Single(r => r.Blid == blid);

    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException("Condition not met within timeout.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync();
                }
            }
            catch
            {
                // ignore
            }

            client.Dispose();
        }

        if (_proxy is not null)
        {
            await _proxy.DisposeAsync();
        }

        foreach (var robot in _robots)
        {
            await robot.DisposeAsync();
        }
    }

    private sealed class StaticRobotSource(IReadOnlyList<ProxyRobotDescriptor> robots) : IProxyRobotSource
    {
        public Task<IReadOnlyList<ProxyRobotDescriptor>> GetRobotsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(robots);
    }
}

/// <summary>An in-memory stand-in for a physical robot: a plain MQTT broker plus a publishing client.</summary>
public sealed class FakeRobot : IAsyncDisposable
{
    private readonly MqttServer _server;
    private readonly MqttClientFactory _factory = new();
    private IMqttClient? _publisher;
    private readonly ConcurrentQueue<(string Topic, byte[] Payload)> _received = new();

    public FakeRobot(string blid, string password, int port)
    {
        Blid = blid;
        Password = password;
        Port = port;

        var options = new MqttServerFactory().CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .Build();

        _server = new MqttServerFactory().CreateMqttServer(options);
        _server.ClientConnectedAsync += args =>
        {
            // The proxy upstream is the only non-publisher client that connects here.
            if (!string.Equals(args.ClientId, PublisherClientId, StringComparison.Ordinal))
            {
                HasUpstream = true;
            }

            return Task.CompletedTask;
        };
        _server.InterceptingPublishAsync += args =>
        {
            if (!string.Equals(args.ClientId, PublisherClientId, StringComparison.Ordinal))
            {
                // Record messages the proxy relayed to the robot (client -> robot path).
            }

            _received.Enqueue((args.ApplicationMessage.Topic, args.ApplicationMessage.Payload.ToArray()));
            return Task.CompletedTask;
        };
    }

    private string PublisherClientId => $"{Blid}-robotpub";

    public string Blid { get; }
    public string Password { get; }
    public int Port { get; }
    public bool HasUpstream { get; private set; }

    public async Task StartAsync()
    {
        await _server.StartAsync();
        _publisher = _factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", Port)
            .WithClientId(PublisherClientId)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();
        await _publisher.ConnectAsync(options, CancellationToken.None);
    }

    /// <summary>Publishes a message as if the robot emitted it; the proxy upstream relays it to clients.</summary>
    public async Task EmitAsync(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();
        await _publisher!.PublishAsync(message, CancellationToken.None);
    }

    /// <summary>True when the robot broker has received a client-&gt;robot publish on the given topic.</summary>
    public bool ReceivedTopic(string topic) => _received.Any(m => m.Topic == topic);

    public async ValueTask DisposeAsync()
    {
        if (_publisher is not null)
        {
            try
            {
                await _publisher.DisconnectAsync();
            }
            catch
            {
                // ignore
            }

            _publisher.Dispose();
        }

        await _server.StopAsync();
        _server.Dispose();
    }
}

/// <summary>An MQTT client that records every message it receives for assertions.</summary>
public sealed class CollectingClient
{
    private readonly ConcurrentQueue<ReceivedMessage> _messages = new();

    public CollectingClient(IMqttClient client)
    {
        Client = client;
        client.ApplicationMessageReceivedAsync += args =>
        {
            _messages.Enqueue(new ReceivedMessage(
                args.ApplicationMessage.Topic,
                Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray())));
            return Task.CompletedTask;
        };
    }

    public IMqttClient Client { get; }

    public IReadOnlyCollection<ReceivedMessage> Messages => _messages.ToArray();

    public async Task SubscribeAsync(params string[] topics)
    {
        var builder = new MqttClientFactory().CreateSubscribeOptionsBuilder();
        foreach (var topic in topics)
        {
            builder = builder.WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce));
        }

        await Client.SubscribeAsync(builder.Build(), CancellationToken.None);
    }

    public async Task PublishAsync(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();
        await Client.PublishAsync(message, CancellationToken.None);
    }

    public ReceivedMessage? Find(string topic) => _messages.FirstOrDefault(m => m.Topic == topic);

    public bool Has(string topic) => _messages.Any(m => m.Topic == topic);
}

public readonly record struct ReceivedMessage(string Topic, string Payload);
