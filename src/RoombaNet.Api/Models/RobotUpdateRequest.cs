namespace RoombaNet.Api.Models;

public class RobotUpdateRequest
{
    public string? Ip { get; init; }
    public int? Port { get; init; }
    public string? Password { get; init; }
}
