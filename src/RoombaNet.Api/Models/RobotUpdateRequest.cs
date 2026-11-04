namespace RoombaNet.Api.Models;

public class RobotUpdateRequest
{
    public string? Name { get; init; }
    public string? Ip { get; init; }
    public int? Port { get; init; }
    public string? Password { get; init; }

    // False to reach the robot through a plain MQTT proxy.
    public bool? UseTls { get; init; }
}
