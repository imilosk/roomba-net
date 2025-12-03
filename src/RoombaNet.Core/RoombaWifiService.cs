using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Core;

public class RoombaWifiService : IRoombaWifiService
{
    private readonly ILogger<RoombaWifiService> _logger;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;

    public RoombaWifiService(
        ILogger<RoombaWifiService> logger,
        MqttClientFactory mqttClientFactory,
        RoombaSettings roombaSettings)
    {
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = roombaSettings;
    }

    public async Task<bool> ConfigureWifiAsync(
        WifiConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        IMqttClient? client = null;
        try
        {
            _logger.LogInformation("Starting Wi-Fi configuration for SSID: {Ssid}", request.Ssid);

            // Connect to Roomba's AP network (192.168.10.1)
            client = _mqttClientFactory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(RoombaWifi.DefaultApAddress, RoombaWifi.DefaultApPort)
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

            _logger.LogInformation("Connecting to Roomba's AP network at {Address}...", RoombaWifi.DefaultApAddress);
            var connectResult = await client.ConnectAsync(options, cancellationToken);

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("Failed to connect to Roomba's AP: {ResultCode}", connectResult.ResultCode);
                return false;
            }

            await SendWifiConfiguration(
                client,
                request,
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

    private async Task SendWifiConfiguration(
        IMqttClient client,
        WifiConfigurationRequest request,
        CancellationToken cancellationToken
    )
    {
        var messages = new List<(string topic, object state)>();

        messages.Add((RoombaTopic.WifiCtl, new WActivateState
        {
            WActivate = false,
        }));

        var utcTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        messages.Add((RoombaTopic.WifiCtl, new UtcTimeState
        {
            UtcTime = utcTime,
        }));

        var localTimeOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
        messages.Add((RoombaTopic.WifiCtl, new LocalTimeOffsetState
        {
            LocalTimeOffset = localTimeOffset,
        }));

        if (!string.IsNullOrEmpty(request.Timezone))
        {
            _logger.LogInformation("Setting timezone: {Timezone}", request.Timezone);
            messages.Add((RoombaTopic.Delta, new TimezoneState
            {
                Timezone = request.Timezone,
            }));
        }

        if (!string.IsNullOrEmpty(request.Country))
        {
            _logger.LogInformation("Setting country: {Country}", request.Country);
            messages.Add((RoombaTopic.Delta, new CountryState
            {
                Country = request.Country,
            }));
        }

        if (!string.IsNullOrEmpty(request.RobotName))
        {
            _logger.LogInformation("Setting robot name: {RobotName}", request.RobotName);
            messages.Add((RoombaTopic.Delta, new NameState
            {
                Name = request.RobotName,
            }));
        }

        var credentials = new WifiCredentials
        {
            Sec = 7, // WPA2-PSK
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

        messages.Add((RoombaTopic.WifiCtl, new WlcfgState
        {
            Wlcfg = credentials,
        }));

        messages.Add((RoombaTopic.WifiCtl, new ChkSsidState
        {
            ChkSsid = true,
        }));
        messages.Add((RoombaTopic.WifiCtl, new WActivateState
        {
            WActivate = true,
        }));
        messages.Add((RoombaTopic.WifiCtl, new GetState
        {
            Get = "netinfo",
        }));

        messages.Add((RoombaTopic.WifiCtl, new UapState
        {
            Uap = false,
        }));

        foreach (var (topic, state) in messages)
        {
            var payload = SerializeState(state);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await client.PublishAsync(message, cancellationToken);
            _logger.LogDebug("Published to {Topic}: {Payload}", topic, payload);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
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
