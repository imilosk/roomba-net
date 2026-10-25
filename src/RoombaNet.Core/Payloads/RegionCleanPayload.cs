using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>
/// A single region (room or zone) within a persistent map to clean.
/// </summary>
public sealed class Region
{
    [JsonPropertyName("region_id")]
    public string RegionId { get; set; } = string.Empty;

    /// <summary>Region type: "rid" for a room, "zid" for a user-defined zone.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "rid";
}

/// <summary>
/// Payload for a region/zone targeted clean, published to the <c>cmd</c> topic.
/// </summary>
public sealed class RegionCleanPayload
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "start";

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("initiator")]
    public string Initiator { get; set; } = string.Empty;

    /// <summary>1 to clean regions in the given order, 0 to let the robot optimize the order.</summary>
    [JsonPropertyName("ordered")]
    public int Ordered { get; set; } = 1;

    [JsonPropertyName("pmap_id")]
    public string PmapId { get; set; } = string.Empty;

    [JsonPropertyName("regions")]
    public List<Region> Regions { get; set; } = [];

    [JsonPropertyName("user_pmapv_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserPmapvId { get; set; }
}
