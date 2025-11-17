namespace RoombaNet.Api.Models;

public record SettingsResponse(
    bool Success,
    string Setting,
    string Message,
    DateTime Timestamp,
    string ExecutionId,
    string? Error = null,
    string? Details = null
);
