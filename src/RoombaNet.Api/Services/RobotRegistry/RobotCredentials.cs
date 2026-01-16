namespace RoombaNet.Api.Services.RobotRegistry;

public record RobotCredentials(
    string Blid,
    string Ip,
    int Port,
    string Password
);
