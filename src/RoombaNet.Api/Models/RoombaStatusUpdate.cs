namespace RoombaNet.Api.Models;

public record RoombaStatusUpdate(
    string Topic,
    string PayloadJson,
    DateTime Timestamp
);
