namespace RoombaNet.Core.Constants;

/// <summary>
/// Constants related to Roomba Wi-Fi configuration and AP mode
/// </summary>
public static class RoombaApDefaults
{
    /// <summary>
    /// Default IP address of Roomba when in Access Point mode
    /// </summary>
    public const string DefaultApAddress = "192.168.10.1";

    /// <summary>
    /// Default MQTT port for Roomba AP mode
    /// </summary>
    public const int DefaultApPort = 8883;

    /// <summary>
    /// Default firmware version for modern Roombas (v3+)
    /// Used to determine if SSID/password should be hex-encoded
    /// </summary>
    public const int DefaultFirmwareVersion = 3;
}
