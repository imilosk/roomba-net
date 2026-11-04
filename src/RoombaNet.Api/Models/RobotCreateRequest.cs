namespace RoombaNet.Api.Models;

public class RobotCreateRequest
{
    public string Blid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Ip { get; init; } = string.Empty;
    public int? Port { get; init; }
    public string? Password { get; init; }

    // False to reach the robot through a plain MQTT proxy. Null keeps the stored value (new robots: true).
    public bool? UseTls { get; init; }
}
