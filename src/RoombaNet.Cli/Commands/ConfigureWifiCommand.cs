using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class ConfigureWifiCommand : Command
{
    private readonly IRoombaWifiClient _wifiClient;
    private readonly CancellationToken _cancellationToken;

    public ConfigureWifiCommand(
        IRoombaWifiClient wifiClient,
        CancellationToken cancellationToken = default
    ) : base("configure-wifi", "Configure Roomba Wi-Fi settings (connect to Roomba's network first)")
    {
        _wifiClient = wifiClient;
        _cancellationToken = cancellationToken;

        var ssidOption = new Option<string>("--ssid")
        {
            Description = "Wi-Fi network SSID",
        };

        var passwordOption = new Option<string>("--password")
        {
            Description = "Wi-Fi network password",
        };

        var robotNameOption = new Option<string?>("--robot-name")
        {
            Description = "Name for the robot (optional)",
        };

        var timezoneOption = new Option<string?>("--timezone")
        {
            Description = "IANA timezone (e.g., America/New_York) (optional)",
        };

        var countryOption = new Option<string?>("--country")
        {
            Description = "Country code (e.g., US, GB) (optional)",
        };

        Add(ssidOption);
        Add(passwordOption);
        Add(robotNameOption);
        Add(timezoneOption);
        Add(countryOption);

        SetAction(async parseResult =>
        {
            var ssid = parseResult.GetValue(ssidOption);
            var password = parseResult.GetValue(passwordOption);
            var robotName = parseResult.GetValue(robotNameOption);
            var timezone = parseResult.GetValue(timezoneOption);
            var country = parseResult.GetValue(countryOption);

            if (string.IsNullOrEmpty(ssid))
            {
                Console.WriteLine("Error: --ssid is required");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: --password is required");
                return;
            }

            await ConfigureWifi(ssid, password, robotName, timezone, country);
        });
    }

    private async Task ConfigureWifi(string ssid, string password, string? robotName, string? timezone, string? country)
    {
        Console.WriteLine("Configuring Wi-Fi...");
        Console.WriteLine($"SSID: {ssid}");

        if (!string.IsNullOrEmpty(robotName))
            Console.WriteLine($"Robot Name: {robotName}");

        if (!string.IsNullOrEmpty(timezone))
            Console.WriteLine($"Timezone: {timezone}");

        if (!string.IsNullOrEmpty(country))
            Console.WriteLine($"Country: {country}");

        Console.WriteLine();

        var success = await _wifiClient.ConfigureWifiAsync(
            ssid,
            password,
            robotName,
            timezone,
            country,
            _cancellationToken);

        if (success)
        {
            Console.WriteLine("Wi-Fi configuration sent successfully!");
            Console.WriteLine("Your Roomba should connect to the network in about a minute.");
            Console.WriteLine("You can now reconnect to your regular Wi-Fi network.");
        }
        else
        {
            Console.WriteLine("Failed to configure Wi-Fi. Make sure you're connected to the Roomba's network.");
        }
    }
}
