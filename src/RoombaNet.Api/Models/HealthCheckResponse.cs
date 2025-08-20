namespace RoombaNet.Api.Models;

public record HealthCheckResponse(
    bool IsHealthy,
    string Status,
    DateTime Timestamp,
    string? Error = null
);