namespace RoombaNet.Api.Models;

public record RoombaStatusUpdate(
    string Topic,
    string Payload,
    DateTime Timestamp
);
