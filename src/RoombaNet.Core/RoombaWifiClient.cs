using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using RoombaNet.Core.Payloads;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Core;

public class RoombaWifiClient : IRoombaWifiClient
{
    private const string RoombaApAddress = "192.168.10.1";
    private const int RoombaApPort = 8883;

    private readonly ILogger<RoombaWifiClient> _logger;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;

    public RoombaWifiClient(
        ILogger<RoombaWifiClient> logger,
        MqttClientFactory mqttClientFactory,
        RoombaSettings roombaSettings)
    {
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = roombaSettings;
    }

    public async Task<bool> ConfigureWifiAsync(
        string wifiSsid,
        string wifiPassword,
        string? robotName = null,
        string? timezone = null,
        string? country = null,
        CancellationToken cancellationToken = default)
    {
        IMqttClient? client = null;
        try
        {
            _logger.LogInformation("Starting Wi-Fi configuration for SSID: {Ssid}", wifiSsid);

            // Connect to Roomba's AP network (192.168.10.1)
            client = _mqttClientFactory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(RoombaApAddress, RoombaApPort)
                .WithClientId(_roombaSettings.Blid)
                .WithCredentials(_roombaSettings.Blid, _roombaSettings.Password)
                .WithCleanSession()
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithCertificateValidationHandler(_ => true);
                })
                .Build();

            _logger.LogInformation("Connecting to Roomba's AP network at {Address}...", RoombaApAddress);
            var connectResult = await client.ConnectAsync(options, cancellationToken);

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("Failed to connect to Roomba's AP: {ResultCode}", connectResult.ResultCode);
                return false;
            }

            // Assume firmware v3+ for most modern Roombas
            const int fwVersion = 3;

            // Send Wi-Fi configuration messages
            await SendWifiConfigurationAsync(
                client,
                wifiSsid,
                wifiPassword,
                robotName,
                timezone,
                country,
                fwVersion,
                cancellationToken
            );

            _logger.LogInformation("Wi-Fi configuration sent successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure Wi-Fi");
            return false;
        }
        finally
        {
            if (client is not null && client.IsConnected)
            {
                await client.DisconnectAsync(cancellationToken: cancellationToken);
            }

            client?.Dispose();
        }
    }

    private async Task SendWifiConfigurationAsync(
        IMqttClient client,
        string wifiSsid,
        string wifiPassword,
        string? robotName,
        string? timezone,
        string? country,
        int fwVersion,
        CancellationToken cancellationToken
    )
    {
        var messages = new List<(string topic, object state)>();

        // Deactivate current Wi-Fi
        messages.Add(("wifictl", new WActivateState
        {
            WActivate = false
        }));

        // Set time
        var utcTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        messages.Add(("wifictl", new UtcTimeState
        {
            UtcTime = utcTime
        }));

        // Set local time offset (in minutes)
        var localTimeOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
        messages.Add(("wifictl", new LocalTimeOffsetState
        {
            LocalTimeOffset = localTimeOffset
        }));

        // Set timezone if provided
        if (!string.IsNullOrEmpty(timezone))
        {
            _logger.LogInformation("Setting timezone: {Timezone}", timezone);
            messages.Add(("delta", new TimezoneState
            {
                Timezone = timezone
            }));
        }

        // Set country if provided
        if (!string.IsNullOrEmpty(country))
        {
            _logger.LogInformation("Setting country: {Country}", country);
            messages.Add(("delta", new CountryState
            {
                Country = country
            }));
        }

        // Set robot name if provided
        if (!string.IsNullOrEmpty(robotName))
        {
            _logger.LogInformation("Setting robot name: {RobotName}", robotName);
            messages.Add(("delta", new NameState
            {
                Name = robotName
            }));
        }

        // Configure Wi-Fi credentials
        var credentials = new WifiCredentials
        {
            Sec = 7, // WPA2-PSK
        };

        if (fwVersion == 2)
        {
            // Firmware v2: plain text
            credentials.Ssid = wifiSsid;
            credentials.Pass = wifiPassword;
        }
        else
        {
            // Firmware v3+: hex-encoded
            credentials.Ssid = ToHex(wifiSsid);
            credentials.Pass = ToHex(wifiPassword);
        }

        messages.Add(("wifictl", new WlcfgState
        {
            Wlcfg = credentials
        }));

        // Check SSID and activate
        messages.Add(("wifictl", new ChkSsidState
        {
            ChkSsid = true
        }));
        messages.Add(("wifictl", new WActivateState
        {
            WActivate = true
        }));
        messages.Add(("wifictl", new GetState
        {
            Get = "netinfo"
        }));

        // Disable access point mode
        messages.Add(("wifictl", new UapState
        {
            Uap = false
        }));

        // Publish messages with delay
        foreach (var (topic, state) in messages)
        {
            var payload = SerializeState(state);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await client.PublishAsync(message, cancellationToken);
            _logger.LogDebug("Published to {Topic}: {Payload}", topic, payload);
            await Task.Delay(1000, cancellationToken); // 1 second delay between messages
        }
    }

    private static string SerializeState(object state)
    {
        return state switch
        {
            WActivateState s => JsonSerializer.Serialize(
                new WifiConfigMessage<WActivateState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageWActivateState),
            UtcTimeState s => JsonSerializer.Serialize(
                new WifiConfigMessage<UtcTimeState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageUtcTimeState),
            LocalTimeOffsetState s => JsonSerializer.Serialize(
                new WifiConfigMessage<LocalTimeOffsetState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageLocalTimeOffsetState),
            TimezoneState s => JsonSerializer.Serialize(
                new WifiConfigMessage<TimezoneState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageTimezoneState),
            CountryState s => JsonSerializer.Serialize(
                new WifiConfigMessage<CountryState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageCountryState),
            NameState s => JsonSerializer.Serialize(
                new WifiConfigMessage<NameState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageNameState),
            WlcfgState s => JsonSerializer.Serialize(
                new WifiConfigMessage<WlcfgState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageWlcfgState),
            ChkSsidState s => JsonSerializer.Serialize(
                new WifiConfigMessage<ChkSsidState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageChkSsidState),
            GetState s => JsonSerializer.Serialize(
                new WifiConfigMessage<GetState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageGetState),
            UapState s => JsonSerializer.Serialize(
                new WifiConfigMessage<UapState>
                {
                    State = s
                },
                WifiConfigJsonContext.Default.WifiConfigMessageUapState),
            _ => throw new ArgumentException($"Unknown state type: {state.GetType()}")
        };
    }

    private static string ToHex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
