using System.Text;
using Microsoft.Extensions.Logging;
using RoombaNet.Core.Payloads;
using RoombaNet.Core.WifiConfig.Commands;

namespace RoombaNet.Core.WifiConfig;

/// <summary>
/// Builds the sequence of WiFi configuration commands based on the request.
/// </summary>
public class WifiConfigCommandBuilder
{
    private readonly ILogger<WifiConfigCommandBuilder> _logger;
    private readonly IMqttPublisher _mqttPublisher;

    public WifiConfigCommandBuilder(
        ILogger<WifiConfigCommandBuilder> logger,
        IMqttPublisher mqttPublisher)
    {
        _logger = logger;
        _mqttPublisher = mqttPublisher;
    }

    /// <summary>
    /// Builds the sequence of commands needed to configure WiFi on the Roomba.
    /// </summary>
    /// <param name="request">The WiFi configuration request.</param>
    /// <returns>An ordered sequence of commands to execute.</returns>
    public IEnumerable<IWifiConfigCommand> BuildCommandSequence(WifiConfigurationRequest request)
    {
        // Step 1: Deactivate WiFi
        yield return new DeactivateWifiCommand(_logger, _mqttPublisher);

        // Step 2: Set UTC time
        var utcTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        yield return new SetUtcTimeCommand(_logger, _mqttPublisher, utcTime);

        // Step 3: Set local time offset
        var localTimeOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
        yield return new SetLocalTimeOffsetCommand(_logger, _mqttPublisher, localTimeOffset);

        // Step 4: Set timezone (optional)
        if (!string.IsNullOrEmpty(request.Timezone))
        {
            yield return new SetTimezoneCommand(_logger, _mqttPublisher, request.Timezone);
        }

        // Step 5: Set country (optional)
        if (!string.IsNullOrEmpty(request.Country))
        {
            yield return new SetCountryCommand(_logger, _mqttPublisher, request.Country);
        }

        // Step 6: Set robot name (optional)
        if (!string.IsNullOrEmpty(request.RobotName))
        {
            yield return new SetRobotNameCommand(_logger, _mqttPublisher, request.RobotName);
        }

        // Step 7: Set WiFi credentials
        var credentials = CreateWifiCredentials(request);
        yield return new SetWifiCredentialsCommand(_logger, _mqttPublisher, credentials);

        // Step 8: Check SSID
        yield return new CheckSsidCommand(_logger, _mqttPublisher);

        // Step 9: Activate WiFi
        yield return new ActivateWifiCommand(_logger, _mqttPublisher);

        // Step 10: Get network info
        yield return new GetNetworkInfoCommand(_logger, _mqttPublisher);

        // Step 11: Disable UAP mode
        yield return new DisableUapCommand(_logger, _mqttPublisher);
    }

    private static WifiCredentials CreateWifiCredentials(WifiConfigurationRequest request)
    {
        var credentials = new WifiCredentials
        {
            Sec = 7, // WPA/WPA2
        };

        if (request.FirmwareVersion == 2)
        {
            credentials.Ssid = request.Ssid;
            credentials.Pass = request.Password;
        }
        else
        {
            credentials.Ssid = ToHex(request.Ssid);
            credentials.Pass = ToHex(request.Password);
        }

        return credentials;
    }

    private static string ToHex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
