using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

public class WifiConfigMessage<T>
{
    [JsonPropertyName("state")]
    public T State { get; set; } = default!;
}

public class WActivateState
{
    [JsonPropertyName("wactivate")]
    public bool WActivate { get; set; }
}

public class UtcTimeState
{
    [JsonPropertyName("utctime")]
    public long UtcTime { get; set; }
}

public class LocalTimeOffsetState
{
    [JsonPropertyName("localtimeoffset")]
    public int LocalTimeOffset { get; set; }
}

public class TimezoneState
{
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;
}

public class CountryState
{
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}

public class NameState
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class WlcfgState
{
    [JsonPropertyName("wlcfg")]
    public WifiCredentials Wlcfg { get; set; } = new();
}

public class WifiCredentials
{
    [JsonPropertyName("sec")]
    public int Sec { get; set; } // 4 = WPA2-PSK

    [JsonPropertyName("ssid")]
    public string Ssid { get; set; } = string.Empty;

    [JsonPropertyName("pass")]
    public string Pass { get; set; } = string.Empty;
}

public class ChkSsidState
{
    [JsonPropertyName("chkssid")]
    public bool ChkSsid { get; set; }
}

public class GetState
{
    [JsonPropertyName("get")]
    public string Get { get; set; } = string.Empty;
}

public class UapState
{
    [JsonPropertyName("uap")]
    public bool Uap { get; set; }
}

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
