namespace RoombaNet.Api.Models;

public class RoombaStatusUpdate
{
    public RoombaStatusUpdate(RoombaState state, DateTime timestamp)
    {
        State = state;
        Timestamp = timestamp;
    }

    public RoombaState State { get; set; }
    public DateTime Timestamp { get; set; }
}