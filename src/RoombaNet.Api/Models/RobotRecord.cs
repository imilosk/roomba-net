namespace RoombaNet.Api.Models;

public record RobotRecord(
    string Blid,
    string Name,
    string Ip,
    int Port,
    bool HasPassword
);
