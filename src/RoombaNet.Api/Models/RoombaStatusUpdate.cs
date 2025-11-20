namespace RoombaNet.Api.Models;

public class RoombaStatusUpdate
{
    public RoombaStatusUpdate(string payload, DateTime timestamp)
    {
        Payload = payload;
        Timestamp = timestamp;
    }

    public string Payload { get; set; }
    public DateTime Timestamp { get; set; }
}