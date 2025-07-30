using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Settings;

namespace RoombaNet.Mqtt;

public class RoombaController : IRoombaController
{
    private const string MessageInitiator = "localApp";

    private readonly ILogger<RoombaController> _logger;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientSubscribeOptions _subscribeOptions;

    public RoombaController(
        ILogger<RoombaController> logger,
        MqttClientFactory mqttClientFactory,
        RoombaSettings roombaSettings
    )
    {
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = roombaSettings;

        _mqttClientOptions = CreateMqttClientChannelOptions(roombaSettings);
        _subscribeOptions = CreateMqttSubscribeOptions();

        _mqttClient = _mqttClientFactory.CreateMqttClient();
    }

    public async Task Subscribe()
    {
        // Set up message received handler to print all messages
        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            PrintMessage(e);
            return Task.CompletedTask;
        };

        _logger.LogInformation("Connecting to MQTT broker at {Ip}:{Port}...", _roombaSettings.Ip, _roombaSettings.Port);

        var result = await _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);

        if (result.ResultCode == MqttClientConnectResultCode.Success)
        {
            _logger.LogInformation(
                "Successfully connected to MQTT broker. Session Present: {IsSessionPresent}. Assigned Client ID: {AssignedClientIdentifier}",
                result.IsSessionPresent,
                result.AssignedClientIdentifier
            );

            await _mqttClient.SubscribeAsync(_subscribeOptions, CancellationToken.None);
            _logger.LogInformation("Subscribed to all topics (#)");
        }
        else
        {
            _logger.LogInformation("Failed to connect to MQTT broker: {ResultCode}, reason: {Reason}",
                result.ResultCode, result.ReasonString);
        }

        _logger.LogInformation("MQTT client finished.");
    }

    public async Task Find()
    {
        var success = await ApiCall(_mqttClient, Topic.Cmd, Command.Find);
        if (success)
        {
            _logger.LogInformation("Command 'find' sent successfully");
        }
        else
        {
            _logger.LogInformation("Failed to send 'find' command");
        }
    }

    private async Task<bool> ApiCall(
        IMqttClient mqttClient,
        string topic,
        string command,
        object? additionalArgs = null
    )
    {
        object cmd;

        if (topic == Topic.Delta)
        {
            cmd = new
            {
                state = command,
            };
        }
        else
        {
            cmd = new
            {
                command,
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                initiator = MessageInitiator,
            };
        }

        if (additionalArgs is not null)
        {
            var cmdDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(cmd)
            );
            var argsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(additionalArgs)
            );

            if (cmdDict is not null && argsDict is not null)
            {
                foreach (var kvp in argsDict)
                {
                    cmdDict[kvp.Key] = kvp.Value;
                }

                cmd = cmdDict;
            }
        }

        var json = System.Text.Json.JsonSerializer.Serialize(cmd);
        _logger.LogInformation("Publishing to topic '{topic}': {json}", topic, json);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, CancellationToken.None);

        return publishResult.IsSuccess;
    }

    private static MqttClientOptions CreateMqttClientChannelOptions(RoombaSettings roombaSettings)
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(roombaSettings.Ip, roombaSettings.Port)
            .WithClientId(roombaSettings.Blid)
            .WithCredentials(roombaSettings.Blid, roombaSettings.Password)
            .WithCleanSession()
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(_ => true);
            })
            .Build();
    }

    private MqttClientSubscribeOptions CreateMqttSubscribeOptions()
    {
        return _mqttClientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(Topic.All)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
    }

    private static void PrintMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine("📥 Message received:");
        Console.WriteLine($"   Topic: {e.ApplicationMessage.Topic}");
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        Console.WriteLine($"   Payload: {payload}");
        Console.WriteLine($"   QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
        Console.WriteLine($"   Retain: {e.ApplicationMessage.Retain}");
        Console.WriteLine($"   Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("   " + new string('-', 50));
    }

    public void Dispose()
    {
        if (_mqttClient.IsConnected)
        {
            _mqttClient.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient is IAsyncDisposable mqttClientAsyncDisposable)
        {
            await mqttClientAsyncDisposable.DisposeAsync();
        }
        else
        {
            _mqttClient.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}