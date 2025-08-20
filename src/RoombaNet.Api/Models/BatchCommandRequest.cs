namespace RoombaNet.Api.Models;

public record BatchCommandRequest(
    string[] Commands,
    bool Sequential = true
);