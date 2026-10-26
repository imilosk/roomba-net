using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

public class RoombaSettingsService : IRoombaSettingsService
{
    private readonly ILogger<RoombaSettingsService> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly IMqttPublisher _mqttPublisher;

    public RoombaSettingsService(
        ILogger<RoombaSettingsService> logger,
        IRoombaConnectionManager connectionManager,
        IMqttPublisher mqttPublisher
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _mqttPublisher = mqttPublisher;
    }

    public async Task SetChildLock(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(RoombaSetting.ChildLock, enable, cancellationToken);
    }

    public async Task SetBinPause(bool enable, CancellationToken cancellationToken = default)
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

    public Task SetRankOverlap(int value, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.RankOverlap, value, cancellationToken);
    }

    public Task SetChargingLightPattern(int value, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.ChargingLightPattern, value, cancellationToken);
    }

    public Task SetPadWetness(int disposable, CancellationToken cancellationToken = default)
    {
        var wetness = new PadWetnessSetting
        {
            Disposable = disposable,
        };

        return SetSetting(RoombaSetting.PadWetness, wetness, cancellationToken);
    }

    public async Task SetCleaningPower(RoombaCleaningPower power, CancellationToken cancellationToken = default)
    {
        switch (power)
        {
            case RoombaCleaningPower.Eco:
                await SetSetting(RoombaSetting.CarpetBoost, false, cancellationToken);
                await SetSetting(RoombaSetting.VacHigh, false, cancellationToken);
                break;
            case RoombaCleaningPower.Auto:
                await SetSetting(RoombaSetting.CarpetBoost, true, cancellationToken);
                await SetSetting(RoombaSetting.VacHigh, false, cancellationToken);
                break;
            case RoombaCleaningPower.Performance:
                await SetSetting(RoombaSetting.CarpetBoost, false, cancellationToken);
                await SetSetting(RoombaSetting.VacHigh, true, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(power), power, null);
        }
    }

    public Task SetEdgeClean(bool enable, CancellationToken cancellationToken = default)
    {
        // openOnly=true means "open areas only" (edge cleaning OFF), so it is the inverse.
        return SetSetting(RoombaSetting.OpenOnly, !enable, cancellationToken);
    }

    public Task SetName(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        return SetSetting(RoombaSetting.Name, name, cancellationToken);
    }

    public Task SetEcoCharge(bool enable, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.EcoCharge, enable, cancellationToken);
    }

    public Task SetMapUpload(bool enable, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.MapUploadAllowed, enable, cancellationToken);
    }

    public Task SetMapLearning(bool enable, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.PmapLearningAllowed, enable, cancellationToken);
    }

    public Task SetContinuousMapLearning(bool enable, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.PmapContinuousLearning, enable, cancellationToken);
    }

    public Task SetBinTypeDetect(int value, CancellationToken cancellationToken = default)
    {
        return SetSetting(RoombaSetting.BinTypeDetect, value, cancellationToken);
    }

    public Task SetPerimeterPass(bool enable, CancellationToken cancellationToken = default)
    {
        // noPP=true means "no perimeter pass", so it is the inverse of enabling it.
        return SetSetting(RoombaSetting.NoPerimeterPass, !enable, cancellationToken);
    }

    public Task SetTimezone(string ianaTimezone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ianaTimezone))
        {
            throw new ArgumentException("Timezone must not be empty.", nameof(ianaTimezone));
        }

        return SetSetting(RoombaSetting.Timezone, ianaTimezone, cancellationToken);
    }

    public Task SetVolume(int volume, CancellationToken cancellationToken = default)
    {
        if (volume is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Volume must be 0-100.");
        }

        return SetSetting(RoombaSetting.Audio, new AudioSetting { Volume = volume }, cancellationToken);
    }

    public Task SetLanguage(string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Language code must not be empty.", nameof(languageCode));
        }

        return SetSetting(
            RoombaSetting.Languages,
            new LanguageSetting { SelectedLanguage = languageCode },
            cancellationToken);
    }

    public Task SetSchedule(
        IReadOnlyList<string> cycle,
        IReadOnlyList<int> hours,
        IReadOnlyList<int> minutes,
        CancellationToken cancellationToken = default)
    {
        if (cycle.Count != 7 || hours.Count != 7 || minutes.Count != 7)
        {
            throw new ArgumentException("Schedule must have exactly 7 entries per field (Sun..Sat).");
        }

        // Group enabled days that share the same time into one cleanSchedule2 event.
        var byTime = new Dictionary<(int, int), List<int>>();
        for (var day = 0; day < 7; day++)
        {
            if (cycle[day] != "start")
            {
                continue;
            }

            var key = (hours[day], minutes[day]);
            if (!byTime.TryGetValue(key, out var days))
            {
                days = [];
                byTime[key] = days;
            }

            days.Add(day);
        }

        var events = byTime.Select(kv => new CleanScheduleEvent
        {
            Enabled = true,
            Type = 0,
            Start = new ScheduleStart { Day = [.. kv.Value], Hour = kv.Key.Item1, Min = kv.Key.Item2 },
            Cmd = new ScheduleCmd { Command = "start", Ordered = 1 },
        }).ToArray();

        return SetSetting(RoombaSetting.Schedule, events, cancellationToken);
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

        if (typeof(T) == typeof(PadWetnessSetting))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadPadWetnessSetting;
        }

        if (typeof(T) == typeof(AudioSetting))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadAudioSetting;
        }

        if (typeof(T) == typeof(LanguageSetting))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadLanguageSetting;
        }

        if (typeof(T) == typeof(CleanScheduleEvent[]))
        {
            return (JsonTypeInfo<SettingPayload<T>>)(object)RoombaJsonContext.Default.SettingPayloadCleanScheduleEventArray;
        }

        throw new NotSupportedException(
            $"Type {typeof(T).Name} is not supported for settings serialization. Add it to RoombaJsonContext."
        );
    }
}
