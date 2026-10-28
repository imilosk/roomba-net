using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Transport.Mqtt;

public class RoombaConnectionManager : IRoombaConnectionManager
{
    private readonly ILogger<RoombaConnectionManager> _logger;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttClientOptions;
    private string? _lastConnectionError;

    public bool IsConnected => _mqttClient.IsConnected;

    public RoombaConnectionManager(
        ILogger<RoombaConnectionManager> logger,
        MqttClientFactory mqttClientFactory,
        RoombaSettings settings
    )
    {
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = settings;

        _mqttClient = mqttClientFactory.CreateMqttClient();
        _mqttClientOptions = CreateMqttClientChannelOptions(settings);
    }

    public async Task<IMqttClient> GetClient(CancellationToken cancellationToken = default)
    {
        var success = await EnsureConnectedAsync(cancellationToken);

        return !success
            ? throw new InvalidOperationException(_lastConnectionError ?? "Failed to connect to MQTT broker.")
            : _mqttClient;
    }

    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return true;
        }

        _logger.LogInformation("Connecting to MQTT broker at {Ip}:{Port}...", _roombaSettings.Ip, _roombaSettings.Port);

        var result = await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);

        if (result.ResultCode == MqttClientConnectResultCode.Success)
        {
            _lastConnectionError = null;
            _logger.LogInformation(
                "Successfully connected to MQTT broker. Session Present: {IsSessionPresent}. Assigned Client ID: {AssignedClientIdentifier}",
                result.IsSessionPresent,
                result.AssignedClientIdentifier
            );
            return true;
        }

        _lastConnectionError = $"Failed to connect to MQTT broker: {result.ResultCode}, reason: {result.ReasonString}";
        _logger.LogError(
            "Failed to connect to MQTT broker: {ResultCode}, reason: {Reason}",
            result.ResultCode,
            result.ReasonString
        );
        return false;
    }

    private static MqttClientOptions CreateMqttClientChannelOptions(RoombaSettings roombaSettings)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(roombaSettings.Ip, roombaSettings.Port)
            .WithCleanSession()
            .WithProtocolVersion(MqttProtocolVersion.V311);

        if (!roombaSettings.UseTls)
        {
            // Plain connection to a proxy shared by several clients: a fixed client id would make
            // each new connection kick the previous one off, so make it unique per connection.
            builder.WithClientId($"{roombaSettings.Blid}-{Guid.NewGuid().ToString("N")[..12]}");
            if (!string.IsNullOrEmpty(roombaSettings.Password))
            {
                builder.WithCredentials(roombaSettings.Blid, roombaSettings.Password);
            }

            return builder.Build();
        }

        return builder
            .WithClientId(roombaSettings.Blid)
            .WithCredentials(roombaSettings.Blid, roombaSettings.Password)
            .WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(_ => true);
            })
            .Build();
    }

    public async Task RequestShadow(CancellationToken cancellationToken = default)
    {
        var client = await GetClient(cancellationToken);
        var shadowRoot = $"$aws/things/{_roombaSettings.Blid}/shadow";

        // A "#" subscription does not deliver reserved $-topics, so subscribe to the shadow
        // tree explicitly to receive the full shadow and the get/accepted response.
        var subscribeOptions = _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic($"{shadowRoot}/#")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
        await client.SubscribeAsync(subscribeOptions, cancellationToken);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{shadowRoot}/get")
            .WithPayload(Array.Empty<byte>())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        await client.PublishAsync(message, cancellationToken);
        _logger.LogDebug("Subscribed to {Root}/# and requested full shadow", shadowRoot);
    }

    public MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic)
    {
        return _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _mqttClient.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        await ValueTask.CompletedTask;
    }
}