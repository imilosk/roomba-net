using System.Text.Json.Serialization;

namespace RoombaNet.Core.WifiConfig;

public class WifiConfig
{
    [JsonPropertyName("sec")]
    public int? Sec { get; set; }

    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }
}