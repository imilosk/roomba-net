using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Settings;

namespace RoombaNet.Mqtt;

public class Service : IService
{
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly RoombaSettings _roombaSettings;
    private readonly MqttClientOptions _mqttClientOptions;

    public Service(MqttClientFactory mqttClientFactory, RoombaSettings roombaSettings)
    {
        _mqttClientFactory = mqttClientFactory;
        _roombaSettings = roombaSettings;

        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_roombaSettings.Ip, _roombaSettings.Port)
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
    }

    public async Task Main()
    {
        Console.WriteLine("Starting MQTT Client...");

        using var mqttClient = _mqttClientFactory.CreateMqttClient();

        // Set up message received handler to print all messages
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine($"📥 Message received:");
            Console.WriteLine($"   Topic: {e.ApplicationMessage.Topic}");
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            Console.WriteLine($"   Payload: {payload}");
            Console.WriteLine($"   QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
            Console.WriteLine($"   Retain: {e.ApplicationMessage.Retain}");
            Console.WriteLine($"   Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("   " + new string('-', 50));

            return Task.CompletedTask;
        };

        try
        {
            Console.WriteLine($"Connecting to MQTT broker at {_roombaSettings.Ip}:{_roombaSettings.Port}...");

            var result = await mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);

            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                Console.WriteLine("✅ Successfully connected to MQTT broker!");
                Console.WriteLine($"Session Present: {result.IsSessionPresent}");
                Console.WriteLine($"Assigned Client ID: {result.AssignedClientIdentifier}");

                var subscribeOptions = _mqttClientFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f
                        .WithTopic("#")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .Build();

                await mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
                Console.WriteLine("📡 Subscribed to all topics (#)");

                // Equivalent to _apiCall('cmd', 'start')
                // Console.WriteLine("🚀 Sending find command...");
                // var success = await ApiCall("cmd", "find");
                // Console.WriteLine(success ? "✅ Command sent successfully" : "❌ Failed to send command");

                Console.WriteLine("Waiting for messages... (Press any key to exit)");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine($"❌ Failed to connect to MQTT broker: {result.ResultCode}");
                if (result.ReasonString != null)
                {
                    Console.WriteLine($"Reason: {result.ReasonString}");
                }
            }
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
                Console.WriteLine("🔌 Disconnected from MQTT broker");
            }
        }

        Console.WriteLine("MQTT client finished.");
    }

    // API call function equivalent to the Node.js version
    private async Task<bool> ApiCall(
        IMqttClient mqttClient,
        string topic,
        string command,
        object? additionalArgs = null
    )
    {
        object cmd;

        if (topic == "delta")
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
                initiator = "localApp",
            };
        }

        // Merge with additional arguments if provided
        if (additionalArgs != null)
        {
            // Create anonymous object combining cmd and additionalArgs
            var cmdDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(cmd));
            var argsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(additionalArgs));

            if (cmdDict != null && argsDict != null)
            {
                foreach (var kvp in argsDict)
                {
                    cmdDict[kvp.Key] = kvp.Value;
                }

                cmd = cmdDict;
            }
        }

        var json = System.Text.Json.JsonSerializer.Serialize(cmd);
        Console.WriteLine($"📤 Publishing to topic '{topic}': {json}");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, CancellationToken.None);
        return publishResult.IsSuccess;
    }
}