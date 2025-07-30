using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile($"appsettings.{environment}.json", true)
    .AddJsonFile($"secrets.{environment}.json", true)
    .AddEnvironmentVariables()
    .Build();

var roombaIp = configuration["ROOMBA_IP"] ?? throw new InvalidOperationException("ROOMBA_IP environment variable is not set");
var brokerPort = Convert.ToInt32(configuration["ROOMBA_PORT"] ?? throw new InvalidOperationException("ROOMBA_PORT environment variable is not set"));
var username = configuration["ROOMBA_BLID"] ?? throw new InvalidOperationException("ROOMBA_BLID environment variable is not set");
var password = configuration["ROOMBA_PASSWORD"] ?? throw new InvalidOperationException("ROOMBA_PASSWORD environment variable is not set");

Console.WriteLine("Starting MQTT Client...");

var factory = new MqttClientFactory();
using var mqttClient = factory.CreateMqttClient();

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

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(roombaIp, brokerPort)
    .WithClientId(username)
    .WithCredentials(username, password)
    .WithCleanSession()
    .WithProtocolVersion(MqttProtocolVersion.V311)
    .WithTlsOptions(o =>
    {
        o.UseTls();
        o.WithCertificateValidationHandler(_ => true);
    })
    .Build();

try
{
    Console.WriteLine($"Connecting to MQTT broker at {roombaIp}:{brokerPort}...");

    var result = await mqttClient.ConnectAsync(options, CancellationToken.None);

    if (result.ResultCode == MqttClientConnectResultCode.Success)
    {
        Console.WriteLine("✅ Successfully connected to MQTT broker!");
        Console.WriteLine($"Session Present: {result.IsSessionPresent}");
        Console.WriteLine($"Assigned Client ID: {result.AssignedClientIdentifier}");

        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic("#")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
        Console.WriteLine("📡 Subscribed to all topics (#)");

        // API call function equivalent to the Node.js version
        async Task<bool> ApiCall(string topic, string command, object? additionalArgs = null)
        {
            try
            {
                object cmd;
                
                if (topic == "delta")
                {
                    cmd = new { state = command };
                }
                else
                {
                    cmd = new 
                    { 
                        command, 
                        time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 
                        initiator = "localApp" 
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to publish command: {ex.Message}");
                return false;
            }
        }

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
catch (Exception ex)
{
    Console.WriteLine($"❌ Exception occurred: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
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