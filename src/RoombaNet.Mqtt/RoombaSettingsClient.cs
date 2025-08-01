using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Constants;
using RoombaNet.Mqtt.Payloads;

namespace RoombaNet.Mqtt;

public class RoombaSettingsClient : IRoombaSettingsClient
{
    private readonly ILogger<RoombaSettingsClient> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private static readonly MqttApplicationMessageBuilder MessageBuilder = new();

    public RoombaSettingsClient(
        ILogger<RoombaSettingsClient> logger,
        IRoombaConnectionManager connectionManager
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
    }

    public async Task ChildLock(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(RoombaSetting.ChildLock, enable, cancellationToken);
    }

    public async Task BinPause(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(RoombaSetting.BinPause, enable, cancellationToken);
    }

    private async Task SetSetting(
        string setting,
        bool value,
        CancellationToken cancellationToken = default
    )
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        var success = await SettingApiCall(mqttClient, setting, value, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Setting '{Setting}' set successfully", setting);
        }
        else
        {
            _logger.LogError("Failed to set '{Setting}' setting", setting);
        }
    }

    private async Task SetSetting(
        string setting,
        int value,
        CancellationToken cancellationToken = default
    )
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        var success = await SettingApiCall(mqttClient, setting, value, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Setting '{Setting}' set successfully", setting);
        }
        else
        {
            _logger.LogError("Failed to set '{Setting}' setting", setting);
        }
    }

    private async Task SetSetting(
        string setting,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        var success = await SettingApiCall(mqttClient, setting, value, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Setting '{Setting}' set successfully", setting);
        }
        else
        {
            _logger.LogError("Failed to set '{Setting}' setting", setting);
        }
    }

    private async Task<bool> SettingApiCall(
        IMqttClient mqttClient,
        string setting,
        bool value,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = RoombaTopic.Delta;

        var desired = new Dictionary<string, bool>
        {
            {
                setting, value
            },
        };

        var payload = new SettingPayload<bool>(desired);

        return await PublishMessage(mqttClient, topic, payload, cancellationToken);
    }

    private async Task<bool> SettingApiCall(
        IMqttClient mqttClient,
        string setting,
        int value,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = RoombaTopic.Delta;

        var desired = new Dictionary<string, int>
        {
            {
                setting, value
            },
        };

        var payload = new SettingPayload<int>(desired);

        return await PublishMessage(mqttClient, topic, payload, cancellationToken);
    }

    private async Task<bool> SettingApiCall(
        IMqttClient mqttClient,
        string setting,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = RoombaTopic.Delta;

        var desired = new Dictionary<string, string>
        {
            {
                setting, value
            },
        };

        var payload = new SettingPayload<string>(desired);

        return await PublishMessage(mqttClient, topic, payload, cancellationToken);
    }

    private async Task<bool> PublishMessage<T>(
        IMqttClient mqttClient,
        string topic,
        SettingPayload<T> payload,
        CancellationToken cancellationToken = default
    )
    {
        Console.WriteLine(
            $"Publishing to topic '{topic}', settings: {JsonSerializer.Serialize(payload, payload.GetType(), Payloads.RoombaJsonContext.Default)}"
        );

        _logger.LogInformation(
            "Publishing to topic '{Topic}', settings: {@Desired}",
            topic,
            payload.State
        );

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            payload.GetType(),
            Payloads.RoombaJsonContext.Default
        );

        return await PublishMessage(mqttClient, topic, jsonBytes, cancellationToken);
    }

    private static async Task<bool> PublishMessage(
        IMqttClient mqttClient,
        string topic,
        byte[] jsonBytes,
        CancellationToken cancellationToken = default
    )
    {
        var message = MessageBuilder
            .WithTopic(topic)
            .WithPayload(jsonBytes)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, cancellationToken);

        return publishResult.IsSuccess;
    }
}
