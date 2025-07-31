using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Constants;
using RoombaNet.Mqtt.Extensions;

namespace RoombaNet.Mqtt;

public readonly struct CommandPayload : IRoombaPayload
{
    public CommandPayload(string command, long time, string initiator)
    {
        Command = command;
        Time = time;
        Initiator = initiator;
    }

    [JsonPropertyName("command")]
    private string Command { get; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; }

    [JsonPropertyName("initiator")]
    private string Initiator { get; } = string.Empty;
}

public readonly struct StatePayload : IRoombaPayload
{
    public StatePayload(string state)
    {
        State = state;
    }

    [JsonPropertyName("state")]
    public string State { get; } = string.Empty;
}

public interface IRoombaPayload;

public class RoombaClient : IRoombaClient
{
    private const string MessageInitiator = "localApp";

    private readonly ILogger<RoombaClient> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly TimeProvider _timeProvider;
    private static readonly MqttApplicationMessageBuilder MessageBuilder = new();

    public RoombaClient(
        ILogger<RoombaClient> logger,
        IRoombaConnectionManager connectionManager,
        TimeProvider timeProvider
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _timeProvider = timeProvider;
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
        var payload = CreateCommandPayload(topic, command);

        return await PublishMessage(mqttClient, topic, payload);
    }

    private IRoombaPayload CreateCommandPayload(string topic, string command)
    {
        return topic == Topic.Delta
            ? new StatePayload(command)
            : new CommandPayload(
                command,
                _timeProvider.GetTimestampSeconds(),
                MessageInitiator
            );
    }

    private async Task<bool> PublishMessage(IMqttClient mqttClient, string topic, IRoombaPayload payload)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        _logger.LogInformation("Publishing to topic '{topic}': {payload}", topic, payload);

        var message = MessageBuilder
            .WithTopic(topic)
            .WithPayload(jsonBytes)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, CancellationToken.None);

        return publishResult.IsSuccess;
    }
}