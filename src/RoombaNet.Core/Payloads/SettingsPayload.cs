using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

public readonly struct SettingPayload<T>
{
    public SettingPayload(Dictionary<string, T> desired)
    {
        State = desired;
    }

    [JsonPropertyName("state")]
    public Dictionary<string, T> State { get; }
}