using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaNet.Transport.Tls;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

public class RoombaSettingsService : IRoombaSettingsService
{
    private readonly ILogger<RoombaSettingsService> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly IRoombaPasswordClient _roombaPasswordClient;
    private readonly IRoombaDiscoveryClient _roombaDiscoveryClient;

    public RoombaSettingsService(
        ILogger<RoombaSettingsService> logger,
        IRoombaConnectionManager connectionManager,
        IMqttPublisher mqttPublisher,
        IRoombaPasswordClient roombaPasswordClient,
        IRoombaDiscoveryClient roombaDiscoveryClient
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _mqttPublisher = mqttPublisher;
        _roombaPasswordClient = roombaPasswordClient;
        _roombaDiscoveryClient = roombaDiscoveryClient;
    }

    public async Task SetChildLock(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(RoombaSetting.ChildLock, enable, cancellationToken);
    }

    public async Task SetBinPause(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(RoombaSetting.BinPause, enable, cancellationToken);
    }

    public async Task<string> GetPassword(
        string ipAddress,
        int port = 8883,
        CancellationToken cancellationToken = default
    )
    {
        return await _roombaPasswordClient.GetPassword(ipAddress, port, cancellationToken);
    }

    public async Task<List<RoombaInfo>> DiscoverRoombas(CancellationToken cancellationToken = default)
    {
        return await _roombaDiscoveryClient.DiscoverRoombas(cancellationToken: cancellationToken);
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

        var jsonTypeInfo = GetSettingPayloadTypeInfo<T>();

        Console.WriteLine(
            $"Publishing to topic '{topic}', settings: {JsonSerializer.Serialize(payload, jsonTypeInfo)}"
        );

        _logger.LogInformation(
            "Publishing to topic '{Topic}', settings: {@Desired}",
            topic,
            payload.State
        );

        return await _mqttPublisher.PublishAsync(mqttClient, topic, payload, jsonTypeInfo, cancellationToken);
    }

    private static JsonTypeInfo<SettingPayload<T>> GetSettingPayloadTypeInfo<T>()
    {
        // Map to the appropriate JsonTypeInfo based on the generic type
        if (typeof(T) == typeof(bool))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadBoolean;
        }

        if (typeof(T) == typeof(int))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadInt32;
        }

        if (typeof(T) == typeof(string))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadString;
        }

        throw new NotSupportedException(
            $"Type {typeof(T).Name} is not supported for settings serialization. Add it to RoombaJsonContext.");
    }
}
