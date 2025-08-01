using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Core.Constants;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Core;

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

        _subscribeOptions = connectionManager.CreateMqttSubscribeOptions(RoombaTopic.All);
    }

    public async Task Subscribe(
        Action<MqttApplicationMessageReceivedEventArgs> onMessageReceived,
        CancellationToken cancellationToken = default
    )
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            onMessageReceived.Invoke(e);
            return Task.CompletedTask;
        };

        await mqttClient.SubscribeAsync(_subscribeOptions, cancellationToken);
        _logger.LogInformation("Subscribed to all topics (#)");
    }
}