namespace RoombaNet.Api.Models;

public class RobotCreateRequest
{
    public string Blid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Ip { get; init; } = string.Empty;
    public int? Port { get; init; }
    public string? Password { get; init; }
}
