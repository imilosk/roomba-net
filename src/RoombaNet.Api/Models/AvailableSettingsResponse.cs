namespace RoombaNet.Api.Models;

public record AvailableSettingsResponse(string RobotId, IReadOnlyList<string> Settings);
