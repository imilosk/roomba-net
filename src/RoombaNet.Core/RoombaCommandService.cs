using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Extensions;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

public class RoombaCommandService : IRoombaCommandService
{
    private const string MessageInitiator = "localApp";

    private readonly ILogger<RoombaCommandService> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly TimeProvider _timeProvider;

    public RoombaCommandService(
        ILogger<RoombaCommandService> logger,
        IRoombaConnectionManager connectionManager,
        IMqttPublisher mqttPublisher,
        TimeProvider timeProvider
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _mqttPublisher = mqttPublisher;
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

        _logger.LogInformation(
            "Publishing to topic '{Topic}', command: '{Command}', '{Time}'",
            topic,
            payload.Command,
            payload.Time
        );

        return await _mqttPublisher.PublishAsync(
            mqttClient,
            topic,
            payload,
            RoombaJsonContext.Default.CommandPayload,
            cancellationToken
        );
    }
}
