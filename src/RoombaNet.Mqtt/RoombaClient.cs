using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Constants;
using RoombaNet.Mqtt.Extensions;

namespace RoombaNet.Mqtt;

[JsonSerializable(typeof(CommandPayload))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class RoombaJsonContext : JsonSerializerContext;

public readonly struct CommandPayload
{
    public CommandPayload(string command, long time, string initiator)
    {
        Command = command;
        Time = time;
        Initiator = initiator;
    }

    [JsonPropertyName("command")]
    public string Command { get; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; }

    [JsonPropertyName("initiator")]
    public string Initiator { get; } = string.Empty;
}

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
        await ExecuteCommand(Command.Find);
    }

    private async Task ExecuteCommand(string command)
    {
        var mqttClient = await _connectionManager.GetClient();
        const string topic = Topic.Cmd;

        var success = await ApiCall(mqttClient, topic, command);
        if (success)
        {
            _logger.LogInformation("Command 'find' sent successfully to topic '{Topic}'", topic);
        }
        else
        {
            _logger.LogError("Failed to send 'find' command to topic '{Topic}'", topic);
        }
    }

    private async Task<bool> ApiCall(
        IMqttClient mqttClient,
        string topic,
        string command
    )
    {
        var payload = new CommandPayload(
            command,
            _timeProvider.GetTimestampSeconds(),
            MessageInitiator
        );

        return await PublishMessage(mqttClient, topic, payload);
    }

    private async Task<bool> PublishMessage(IMqttClient mqttClient, string topic, CommandPayload payload)
    {
        _logger.LogInformation("Publishing to topic '{Topic}', command: '{Payload}', '{Time}'", topic, payload.Command,
            payload.Time);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            payload.GetType(),
            RoombaJsonContext.Default
        );

        var message = MessageBuilder
            .WithTopic(topic)
            .WithPayload(jsonBytes)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, CancellationToken.None);

        return publishResult.IsSuccess;
    }
}