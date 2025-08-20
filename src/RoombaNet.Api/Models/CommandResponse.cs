namespace RoombaNet.Api.Models;

public record CommandResponse(
    bool Success,
    string Command,
    string Message,
    DateTime Timestamp,
    string ExecutionId,
    string? Error = null,
    string? Details = null
);