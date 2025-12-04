using System.Text.Json.Serialization;

namespace RoombaNet.Transport.Tls;

public class RoombaInfo
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("robotname")]
    public string RobotName { get; set; } = string.Empty;

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("sw")]
    public string SoftwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("nc")]
    public int? Nc { get; set; }

    [JsonPropertyName("proto")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("cap")]
    public Dictionary<string, int>? Capabilities { get; set; }

    /// <summary>
    /// Gets the BLID (robot identifier) extracted from the hostname.
    /// The hostname typically follows the pattern "Roomba-{BLID}" or "iRobot-{BLID}".
    /// </summary>
    [JsonIgnore]
    public string Blid
    {
        get
        {
            if (string.IsNullOrEmpty(Hostname))
                return string.Empty;

            // Hostname format: "Roomba-{BLID}" or "iRobot-{BLID}"
            var parts = Hostname.Split('-', 2);
            return parts.Length == 2 ? parts[1] : string.Empty;
        }
    }
}

[JsonSerializable(typeof(RoombaInfo))]
internal partial class RoombaInfoJsonContext : JsonSerializerContext;
