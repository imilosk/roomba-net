using Microsoft.Extensions.Logging;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;

namespace RoombaNet.Core.WifiConfig.Commands;

public sealed class DeactivateWifiCommand : WifiConfigCommandBase<WActivateState>
{
    public DeactivateWifiCommand(ILogger logger, IMqttPublisher mqttPublisher)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, "Deactivate WiFi",
            WifiConfigJsonContext.Default.WifiConfigMessageWActivateState) { }

    protected override WActivateState CreateState() => new()
    {
        WActivate = false,
    };
}

public sealed class SetUtcTimeCommand : WifiConfigCommandBase<UtcTimeState>
{
    private readonly long _utcTime;

    public SetUtcTimeCommand(ILogger logger, IMqttPublisher mqttPublisher, long utcTime)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, $"Set UTC time: {utcTime}",
            WifiConfigJsonContext.Default.WifiConfigMessageUtcTimeState)
    {
        _utcTime = utcTime;
    }

    protected override UtcTimeState CreateState() => new()
    {
        UtcTime = _utcTime,
    };
}

public sealed class SetLocalTimeOffsetCommand : WifiConfigCommandBase<LocalTimeOffsetState>
{
    private readonly int _offsetMinutes;

    public SetLocalTimeOffsetCommand(ILogger logger, IMqttPublisher mqttPublisher, int offsetMinutes)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, $"Set local time offset: {offsetMinutes} minutes",
            WifiConfigJsonContext.Default.WifiConfigMessageLocalTimeOffsetState)
    {
        _offsetMinutes = offsetMinutes;
    }

    protected override LocalTimeOffsetState CreateState() => new()
    {
        LocalTimeOffset = _offsetMinutes,
    };
}

public sealed class SetTimezoneCommand : WifiConfigCommandBase<TimezoneState>
{
    private readonly string _timezone;

    public SetTimezoneCommand(ILogger logger, IMqttPublisher mqttPublisher, string timezone)
        : base(logger, mqttPublisher, RoombaTopic.Delta, $"Set timezone: {timezone}",
            WifiConfigJsonContext.Default.WifiConfigMessageTimezoneState)
    {
        _timezone = timezone;
    }

    protected override TimezoneState CreateState() => new()
    {
        Timezone = _timezone,
    };
}

public sealed class SetCountryCommand : WifiConfigCommandBase<CountryState>
{
    private readonly string _country;

    public SetCountryCommand(ILogger logger, IMqttPublisher mqttPublisher, string country)
        : base(logger, mqttPublisher, RoombaTopic.Delta, $"Set country: {country}",
            WifiConfigJsonContext.Default.WifiConfigMessageCountryState)
    {
        _country = country;
    }

    protected override CountryState CreateState() => new()
    {
        Country = _country
    };
}

public sealed class SetRobotNameCommand : WifiConfigCommandBase<NameState>
{
    private readonly string _name;

    public SetRobotNameCommand(ILogger logger, IMqttPublisher mqttPublisher, string name)
        : base(logger, mqttPublisher, RoombaTopic.Delta, $"Set robot name: {name}",
            WifiConfigJsonContext.Default.WifiConfigMessageNameState)
    {
        _name = name;
    }

    protected override NameState CreateState() => new()
    {
        Name = _name
    };
}

public sealed class SetWifiCredentialsCommand : WifiConfigCommandBase<WlcfgState>
{
    private readonly WifiCredentials _credentials;

    public SetWifiCredentialsCommand(ILogger logger, IMqttPublisher mqttPublisher, WifiCredentials credentials)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, $"Set WiFi credentials for SSID: {credentials.Ssid}",
            WifiConfigJsonContext.Default.WifiConfigMessageWlcfgState)
    {
        _credentials = credentials;
    }

    protected override WlcfgState CreateState() => new()
    {
        Wlcfg = _credentials,
    };
}

public sealed class CheckSsidCommand : WifiConfigCommandBase<ChkSsidState>
{
    public CheckSsidCommand(ILogger logger, IMqttPublisher mqttPublisher)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, "Check SSID",
            WifiConfigJsonContext.Default.WifiConfigMessageChkSsidState) { }

    protected override ChkSsidState CreateState() => new()
    {
        ChkSsid = true,
    };
}

public sealed class ActivateWifiCommand : WifiConfigCommandBase<WActivateState>
{
    public ActivateWifiCommand(ILogger logger, IMqttPublisher mqttPublisher)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, "Activate WiFi",
            WifiConfigJsonContext.Default.WifiConfigMessageWActivateState) { }

    protected override WActivateState CreateState() => new()
    {
        WActivate = true,
    };
}

public sealed class GetNetworkInfoCommand : WifiConfigCommandBase<GetState>
{
    public GetNetworkInfoCommand(ILogger logger, IMqttPublisher mqttPublisher)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, "Get network info",
            WifiConfigJsonContext.Default.WifiConfigMessageGetState) { }

    protected override GetState CreateState() => new()
    {
        Get = "netinfo",
    };
}

public sealed class DisableUapCommand : WifiConfigCommandBase<UapState>
{
    public DisableUapCommand(ILogger logger, IMqttPublisher mqttPublisher)
        : base(logger, mqttPublisher, RoombaTopic.WifiCtl, "Disable UAP mode",
            WifiConfigJsonContext.Default.WifiConfigMessageUapState) { }

    protected override UapState CreateState() => new()
    {
        Uap = false,
    };
}
