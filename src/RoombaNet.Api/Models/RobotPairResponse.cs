namespace RoombaNet.Api.Models;

public class RobotPairResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public RobotRecord? Robot { get; init; }
}
