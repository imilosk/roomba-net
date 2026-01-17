namespace RoombaNet.Api.Models;

public class WifiConfigureRequest
{
    public string Ssid { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}