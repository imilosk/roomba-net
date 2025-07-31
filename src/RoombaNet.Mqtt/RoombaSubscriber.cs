using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Mqtt.Constants;

namespace RoombaNet.Mqtt;

public class RoombaSubscriber : IRoombaSubscriber
{
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly ILogger<RoombaSubscriber> _logger;
    private readonly MqttClientSubscribeOptions _subscribeOptions;

    public RoombaSubscriber(
        IRoombaConnectionManager connectionManager,
        ILogger<RoombaSubscriber> logger
    )
    {
        _connectionManager = connectionManager;
        _logger = logger;

        _subscribeOptions = connectionManager.CreateMqttSubscribeOptions(Topic.All);
    }

    public async Task Subscribe(Action<MqttApplicationMessageReceivedEventArgs> onMessageReceived)
    {
        var mqttClient = await _connectionManager.GetClient();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            onMessageReceived.Invoke(e);
            return Task.CompletedTask;
        };

        await mqttClient.SubscribeAsync(_subscribeOptions, CancellationToken.None);
        _logger.LogInformation("Subscribed to all topics (#)");
    }
}