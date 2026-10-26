using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>Nested <c>audio</c> delta object. Sent as <c>{"audio":{"volume":N}}</c>.</summary>
public readonly struct AudioSetting
{
    [JsonPropertyName("volume")]
    public int Volume { get; init; }
}
