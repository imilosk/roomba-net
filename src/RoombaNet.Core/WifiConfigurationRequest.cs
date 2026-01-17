namespace RoombaNet.Core;

/// <summary>
/// Request object for configuring Roomba Wi-Fi settings
/// </summary>
public sealed class WifiConfigurationRequest
{
    /// <summary>
    /// Wi-Fi network SSID (required)
    /// </summary>
    public required string Ssid { get; init; }

    /// <summary>
    /// Wi-Fi network password (required)
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Optional name for the robot
    /// </summary>
    public string RobotName { get; init; } = string.Empty;

    /// <summary>
    /// Optional IANA timezone (e.g., America/New_York)
    /// </summary>
    public string Timezone { get; init; } = string.Empty;

    /// <summary>
    /// Optional country code (e.g., US, GB)
    /// </summary>
    public string Country { get; init; } = string.Empty;

    /// <summary>
    /// Firmware version for encoding (defaults to v3+)
    /// </summary>
    public int FirmwareVersion { get; init; } = Constants.RoombaApDefaults.DefaultFirmwareVersion;
}
