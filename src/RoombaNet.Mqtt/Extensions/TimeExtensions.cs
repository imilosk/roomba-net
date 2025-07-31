namespace RoombaNet.Mqtt.Extensions;

public static class TimeExtensions
{
    public static long GetTimestampSeconds(this TimeProvider timeProvider)
    {
        return timeProvider.GetTimestamp() / timeProvider.TimestampFrequency;
    }
}