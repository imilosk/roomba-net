namespace RoombaNet.Api.Models;

using System.Text.Json;

public record RoombaStatusUpdate(
    string Topic,
    JsonElement Payload,
    DateTime Timestamp
);
