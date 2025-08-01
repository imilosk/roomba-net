namespace RoombaNet.Mqtt.Constants;

internal static class Command
{
    public static readonly string Find = nameof(Find).ToLowerInvariant();
    public static readonly string Start = nameof(Start).ToLowerInvariant();
    public static readonly string Stop = nameof(Stop).ToLowerInvariant();
    public static readonly string Pause = nameof(Pause).ToLowerInvariant();
    public static readonly string Resume = nameof(Resume).ToLowerInvariant();
    public static readonly string Dock = nameof(Dock).ToLowerInvariant();
    public static readonly string Evac = nameof(Evac).ToLowerInvariant();
    public static readonly string Off = nameof(Off).ToLowerInvariant();
    public static readonly string Reset = nameof(Reset).ToLowerInvariant();
    public static readonly string Train = nameof(Train).ToLowerInvariant();
}