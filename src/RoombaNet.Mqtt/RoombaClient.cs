using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Mqtt.Constants;
using RoombaNet.Mqtt.Extensions;

namespace RoombaNet.Mqtt;

[JsonSerializable(typeof(CommandPayload))]
[JsonSerializable(typeof(SettingPayload))]
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

public readonly struct SettingPayload
{
    public SettingPayload(string setting, object value)
    {
        Setting = setting;
        Value = value;
    }

    [JsonPropertyName("setting")]
    public string Setting { get; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; }
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

    public async Task Find(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Find, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Start, cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Stop, cancellationToken);
    }

    public async Task Pause(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Pause, cancellationToken);
    }

    public async Task Resume(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Resume, cancellationToken);
    }

    public async Task Dock(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Dock, cancellationToken);
    }

    public async Task Evac(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Evac, cancellationToken);
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Reset, cancellationToken);
    }

    public async Task Train(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(Command.Train, cancellationToken);
    }

    public async Task ChildLock(bool enable, CancellationToken cancellationToken = default)
    {
        await SetSetting(Setting.ChildLock, enable, cancellationToken);
    }

    private async Task ExecuteCommand(string command, CancellationToken cancellationToken = default)
    {
        var mqttClient = await _connectionManager.GetClient();

        var success = await CommandApiCall(mqttClient, command, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Command '{Command}' sent successfully", command);
        }
        else
        {
            _logger.LogError("Failed to send '{Command}' command", command);
        }
    }

    private async Task SetSetting(string setting, object value, CancellationToken cancellationToken = default)
    {
        var mqttClient = await _connectionManager.GetClient();

        var success = await SettingApiCall(mqttClient, setting, value, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Setting '{Setting}' sent successfully", setting);
        }
        else
        {
            _logger.LogError("Failed to send '{Setting}' setting", setting);
        }
    }

    private async Task<bool> CommandApiCall(
        IMqttClient mqttClient,
        string command,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = Topic.Cmd;

        var payload = new CommandPayload(
            command,
            _timeProvider.GetTimestampSeconds(),
            MessageInitiator
        );

        return await PublishMessage(mqttClient, topic, payload, cancellationToken);
    }

    private async Task<bool> SettingApiCall(
        IMqttClient mqttClient,
        string setting,
        object value,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = Topic.Delta;

        var payload = new SettingPayload(
            setting,
            value
        );

        return await PublishMessage(mqttClient, topic, payload, cancellationToken);
    }

    private async Task<bool> PublishMessage(
        IMqttClient mqttClient,
        string topic,
        CommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Publishing to topic '{Topic}', command: '{Payload}', '{Time}'",
            topic,
            payload.Command,
            payload.Time
        );

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            payload.GetType(),
            RoombaJsonContext.Default
        );

        return await PublishMessage(mqttClient, topic, jsonBytes, cancellationToken);
    }

    private async Task<bool> PublishMessage(
        IMqttClient mqttClient,
        string topic,
        SettingPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Publishing to topic '{Topic}', setting: '{Payload}'",
            topic,
            payload.Setting
        );

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            payload.GetType(),
            RoombaJsonContext.Default
        );

        return await PublishMessage(mqttClient, topic, jsonBytes, cancellationToken);
    }

    private static async Task<bool> PublishMessage(
        IMqttClient mqttClient,
        string topic,
        byte[] jsonBytes,
        CancellationToken cancellationToken = default
    )
    {
        var message = MessageBuilder
            .WithTopic(topic)
            .WithPayload(jsonBytes)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(message, cancellationToken);

        return publishResult.IsSuccess;
    }
}