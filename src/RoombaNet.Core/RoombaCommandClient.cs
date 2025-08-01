using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Extensions;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

public class RoombaCommandClient : IRoombaCommandClient
{
    private const string MessageInitiator = "localApp";

    private readonly ILogger<RoombaCommandClient> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly TimeProvider _timeProvider;
    private static readonly MqttApplicationMessageBuilder MessageBuilder = new();

    public RoombaCommandClient(
        ILogger<RoombaCommandClient> logger,
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
        await ExecuteCommand(RoombaCommand.Find, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Start, cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Stop, cancellationToken);
    }

    public async Task Pause(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Pause, cancellationToken);
    }

    public async Task Resume(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Resume, cancellationToken);
    }

    public async Task Dock(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Dock, cancellationToken);
    }

    public async Task Evac(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Evac, cancellationToken);
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Reset, cancellationToken);
    }

    public async Task Train(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Train, cancellationToken);
    }

    private async Task ExecuteCommand(string command, CancellationToken cancellationToken = default)
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

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

    private async Task<bool> CommandApiCall(
        IMqttClient mqttClient,
        string command,
        CancellationToken cancellationToken = default
    )
    {
        const string topic = RoombaTopic.Cmd;

        var payload = new CommandPayload(
            command,
            _timeProvider.GetTimestampSeconds(),
            MessageInitiator
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
