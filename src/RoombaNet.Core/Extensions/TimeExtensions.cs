namespace RoombaNet.Core.Extensions;

public static class TimeExtensions
{
    public static long GetTimestampSeconds(this TimeProvider timeProvider)
    {
        return timeProvider.GetUtcNow().ToUnixTimeSeconds();
    }
}