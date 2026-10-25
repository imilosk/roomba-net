using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>
/// Delta payload that clears all persistent maps by setting <c>pmaps</c> to an empty array.
/// Serializes to <c>{"state":{"pmaps":[]}}</c>.
/// </summary>
public sealed class ClearMapsPayload
{
    [JsonPropertyName("state")]
    public ClearMapsState State { get; set; } = new();
}

public sealed class ClearMapsState
{
    [JsonPropertyName("pmaps")]
    public List<Dictionary<string, string>> Pmaps { get; set; } = [];
}
