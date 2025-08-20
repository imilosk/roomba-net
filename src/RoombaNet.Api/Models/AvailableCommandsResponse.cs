namespace RoombaNet.Api.Models;

public record AvailableCommandsResponse(
    string[] Commands,
    DateTime Timestamp
);