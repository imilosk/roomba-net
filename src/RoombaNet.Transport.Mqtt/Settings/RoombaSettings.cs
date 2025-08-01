namespace RoombaNet.Transport.Mqtt.Settings;

public class RoombaSettings
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 8883; // Default MQTT port
    public string Blid { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}