namespace RoombaNet.Api.Models;

public record RobotRecord(
    string Blid,
    string Ip,
    int Port,
    bool HasPassword
);
