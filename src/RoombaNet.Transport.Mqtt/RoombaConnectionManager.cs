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
            ? throw new InvalidOperationException("Failed to connect to MQTT broker.")
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
            _logger.LogInformation(
                "Successfully connected to MQTT broker. Session Present: {IsSessionPresent}. Assigned Client ID: {AssignedClientIdentifier}",
                result.IsSessionPresent,
                result.AssignedClientIdentifier
            );
            return true;
        }

        _logger.LogError(
            "Failed to connect to MQTT broker: {ResultCode}, reason: {Reason}",
            result.ResultCode,
            result.ReasonString
        );
        return false;
    }

    private static MqttClientOptions CreateMqttClientChannelOptions(RoombaSettings roombaSettings)
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(roombaSettings.Ip, roombaSettings.Port)
            .WithClientId(roombaSettings.Blid)
            .WithCredentials(roombaSettings.Blid, roombaSettings.Password)
            .WithCleanSession()
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(_ => true);
            })
            .Build();
    }

    public MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic)
    {
        return _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
    }

    public void Dispose()
    {
        if (_mqttClient.IsConnected)
        {
            _mqttClient.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient is IAsyncDisposable mqttClientAsyncDisposable)
        {
            await mqttClientAsyncDisposable.DisposeAsync();
        }
        else
        {
            _mqttClient.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}