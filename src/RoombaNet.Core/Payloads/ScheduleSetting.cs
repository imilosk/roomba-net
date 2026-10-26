using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>
/// One entry in the modern <c>cleanSchedule2</c> array (daredevil/sanmarino firmware).
/// Shape reconstructed from the app's native schedule serializer
/// (keys: enabled/type/start{day,hour,min}/cmd{command,ordered}).
/// </summary>
public readonly struct CleanScheduleEvent
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("type")]
    public int Type { get; init; }

    [JsonPropertyName("start")]
    public ScheduleStart Start { get; init; }

    [JsonPropertyName("cmd")]
    public ScheduleCmd Cmd { get; init; }
}

public readonly struct ScheduleStart
{
    /// <summary>Weekday indices (0 = Sunday .. 6 = Saturday) this event fires on.</summary>
    [JsonPropertyName("day")]
    public int[] Day { get; init; }

    [JsonPropertyName("hour")]
    public int Hour { get; init; }

    [JsonPropertyName("min")]
    public int Min { get; init; }
}

public readonly struct ScheduleCmd
{
    [JsonPropertyName("command")]
    public string Command { get; init; }

    [JsonPropertyName("ordered")]
    public int Ordered { get; init; }
}
