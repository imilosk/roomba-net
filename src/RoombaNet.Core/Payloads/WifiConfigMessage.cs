using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>
/// Base wrapper for all Wi-Fi configuration messages.
/// </summary>
public class WifiConfigMessage<T>
{
    [JsonPropertyName("state")]
    public T State { get; set; } = default!;
}

/// <summary>
/// State for activating/deactivating Wi-Fi.
/// </summary>
public class WActivateState
{
    [JsonPropertyName("wactivate")]
    public bool WActivate { get; set; }
}

/// <summary>
/// State for setting UTC time.
/// </summary>
public class UtcTimeState
{
    [JsonPropertyName("utctime")]
    public long UtcTime { get; set; }
}

/// <summary>
/// State for setting local time offset.
/// </summary>
public class LocalTimeOffsetState
{
    [JsonPropertyName("localtimeoffset")]
    public int LocalTimeOffset { get; set; }
}

/// <summary>
/// State for timezone configuration.
/// </summary>
public class TimezoneState
{
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;
}

/// <summary>
/// State for country configuration.
/// </summary>
public class CountryState
{
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// State for robot name configuration.
/// </summary>
public class NameState
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// State for Wi-Fi credentials configuration.
/// </summary>
public class WlcfgState
{
    [JsonPropertyName("wlcfg")]
    public WifiCredentials Wlcfg { get; set; } = new();
}

/// <summary>
/// Wi-Fi credentials with security settings.
/// </summary>
public class WifiCredentials
{
    [JsonPropertyName("sec")]
    public int Sec { get; set; } // 4 = WPA2-PSK

    [JsonPropertyName("ssid")]
    public string Ssid { get; set; } = string.Empty;

    [JsonPropertyName("pass")]
    public string Pass { get; set; } = string.Empty;
}

/// <summary>
/// State for checking SSID.
/// </summary>
public class ChkSsidState
{
    [JsonPropertyName("chkssid")]
    public bool ChkSsid { get; set; }
}

/// <summary>
/// State for getting network info.
/// </summary>
public class GetState
{
    [JsonPropertyName("get")]
    public string Get { get; set; } = string.Empty;
}

/// <summary>
/// State for enabling/disabling access point mode.
/// </summary>
public class UapState
{
    [JsonPropertyName("uap")]
    public bool Uap { get; set; }
}

/// <summary>
/// JSON serializer context for Wi-Fi configuration messages.
/// </summary>
[JsonSerializable(typeof(WifiConfigMessage<WActivateState>))]
[JsonSerializable(typeof(WifiConfigMessage<UtcTimeState>))]
[JsonSerializable(typeof(WifiConfigMessage<LocalTimeOffsetState>))]
[JsonSerializable(typeof(WifiConfigMessage<TimezoneState>))]
[JsonSerializable(typeof(WifiConfigMessage<CountryState>))]
[JsonSerializable(typeof(WifiConfigMessage<NameState>))]
[JsonSerializable(typeof(WifiConfigMessage<WlcfgState>))]
[JsonSerializable(typeof(WifiConfigMessage<ChkSsidState>))]
[JsonSerializable(typeof(WifiConfigMessage<GetState>))]
[JsonSerializable(typeof(WifiConfigMessage<UapState>))]
[JsonSerializable(typeof(WActivateState))]
[JsonSerializable(typeof(UtcTimeState))]
[JsonSerializable(typeof(LocalTimeOffsetState))]
[JsonSerializable(typeof(TimezoneState))]
[JsonSerializable(typeof(CountryState))]
[JsonSerializable(typeof(NameState))]
[JsonSerializable(typeof(WlcfgState))]
[JsonSerializable(typeof(WifiCredentials))]
[JsonSerializable(typeof(ChkSsidState))]
[JsonSerializable(typeof(GetState))]
[JsonSerializable(typeof(UapState))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class WifiConfigJsonContext : JsonSerializerContext { }
