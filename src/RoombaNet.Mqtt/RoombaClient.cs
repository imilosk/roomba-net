using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Constants;
using RoombaNet.Mqtt.Extensions;

namespace RoombaNet.Mqtt;

public class RoombaClient : IRoombaClient
{
    private const string MessageInitiator = "localApp";

    private readonly ILogger<RoombaClient> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly TimeProvider _timeProvider;
    private readonly MqttApplicationMessageBuilder _messageBuilder;

    public RoombaClient(
        ILogger<RoombaClient> logger,
        IRoombaConnectionManager connectionManager,
        TimeProvider timeProvider
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _timeProvider = timeProvider;
        _messageBuilder = new MqttApplicationMessageBuilder();
    }

    public async Task Find()
    {
        var mqttClient = await _connectionManager.GetClient();

        var success = await ApiCall(mqttClient, Topic.Cmd, Command.Find);
        if (success)
        {
            _logger.LogInformation("Command 'find' sent successfully");
        }
        else
        {
            _logger.LogError("Failed to send 'find' command");
        }
    }

    private async Task<bool> ApiCall(
        IMqttClient mqttClient,
        string topic,
        string command
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
                time = _timeProvider.GetTimestampSeconds(),
                initiator = MessageInitiator,
            };
        }

        var json = JsonSerializer.Serialize(cmd);
        _logger.LogInformation("Publishing to topic '{topic}': {json}", topic, json);

        var message = _messageBuilder
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, CancellationToken.None);

        return publishResult.IsSuccess;
    }
}