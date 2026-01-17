using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using RoombaNet.Core.Constants;
using RoombaNet.Core.WifiConfig;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Core;

public class RoombaWifiService : IRoombaWifiService
{
    private readonly ILogger<RoombaWifiService> _logger;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;
    private readonly WifiConfigCommandBuilder _commandBuilder;

    public RoombaWifiService(
        ILogger<RoombaWifiService> logger,
        MqttClientFactory mqttClientFactory,
        RoombaSettings roombaSettings,
        WifiConfigCommandBuilder commandBuilder
    )
    {
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = roombaSettings;
        _commandBuilder = commandBuilder;
    }

    public async Task<bool> ConfigureWifiAsync(
        WifiConfigurationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        IMqttClient? client = null;
        try
        {
            _logger.LogInformation("Starting Wi-Fi configuration for SSID: {Ssid}", request.Ssid);

            // Connect to Roomba's AP network (192.168.10.1)
            client = _mqttClientFactory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(RoombaApDefaults.DefaultApAddress, RoombaApDefaults.DefaultApPort)
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

            _logger.LogDebug("Connecting to Roomba's AP network at {Address}...", RoombaApDefaults.DefaultApAddress);
            var connectResult = await client.ConnectAsync(options, cancellationToken);

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("Failed to connect to Roomba's AP: {ResultCode}", connectResult.ResultCode);
                return false;
            }

            await ExecuteConfigurationCommands(client, request, cancellationToken);

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

    private async Task ExecuteConfigurationCommands(
        IMqttClient client,
        WifiConfigurationRequest request,
        CancellationToken cancellationToken
    )
    {
        var commands = _commandBuilder.BuildCommandSequence(request);

        foreach (var command in commands)
        {
            _logger.LogDebug("Executing: {Description}", command.Description);
            await command.ExecuteAsync(client, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}