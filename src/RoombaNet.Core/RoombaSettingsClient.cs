using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

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

    public async Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default)
    {
        switch (passes)
        {
            case RoombaCleaningPasses.OnePass:
                await SetSetting(RoombaSetting.TwoPass, false, cancellationToken);
                await SetSetting(RoombaSetting.NoAutoPasses, true, cancellationToken);
                break;
            case RoombaCleaningPasses.TwoPass:
                await SetSetting(RoombaSetting.TwoPass, true, cancellationToken);
                await SetSetting(RoombaSetting.NoAutoPasses, true, cancellationToken);
                break;
            case RoombaCleaningPasses.RoomSizeClean:
                await SetSetting(RoombaSetting.TwoPass, false, cancellationToken);
                await SetSetting(RoombaSetting.NoAutoPasses, false, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(passes), passes, null);
        }
    }

    private async Task SetSetting<T>(
        string setting,
        T value,
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

    private async Task<bool> SettingApiCall<T>(
        IMqttClient mqttClient,
        string setting,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = RoombaTopic.Delta;

        var desired = new Dictionary<string, T>
        {
            {
                setting, value
            },
        };

        var payload = new SettingPayload<T>(desired);

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
            $"Publishing to topic '{topic}', settings: {JsonSerializer.Serialize(payload, payload.GetType(), RoombaJsonContext.Default)}"
        );

        _logger.LogInformation(
            "Publishing to topic '{Topic}', settings: {@Desired}",
            topic,
            payload.State
        );

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            payload.GetType(),
            RoombaJsonContext.Default
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
