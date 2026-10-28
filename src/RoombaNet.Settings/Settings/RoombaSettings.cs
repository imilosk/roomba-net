namespace RoombaNet.Settings.Settings;

public class RoombaSettings
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 8883; // Default MQTT port
    public string Blid { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // The robot only speaks TLS. Set to false to connect to a plain MQTT proxy in front of it.
    public bool UseTls { get; set; } = true;
}