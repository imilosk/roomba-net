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

    public async Task Spot(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Spot, cancellationToken);
    }

    public async Task Wake(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Wake, cancellationToken);
    }

    public async Task Sleep(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Sleep, cancellationToken);
    }

    public async Task Off(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Off, cancellationToken);
    }

    public async Task Tidy(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Tidy, cancellationToken);
    }

    public async Task Skip(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.Skip, cancellationToken);
    }

    public async Task StopEvac(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.StopEvac, cancellationToken);
    }

    public async Task WifiOn(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.WifiOn, cancellationToken);
    }

    public async Task WifiOff(CancellationToken cancellationToken = default)
    {
        await ExecuteCommand(RoombaCommand.WifiOff, cancellationToken);
    }

    public async Task Send(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command must not be empty.", nameof(command));
        }

        await ExecuteCommand(command.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task CleanRegions(
        string pmapId,
        IReadOnlyList<Region> regions,
        string? userPmapvId = null,
        bool ordered = true,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(pmapId))
        {
            throw new ArgumentException("pmapId is required for a region clean.", nameof(pmapId));
        }

        if (regions is null || regions.Count == 0)
        {
            throw new ArgumentException("At least one region is required for a region clean.", nameof(regions));
        }

        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        var payload = new RegionCleanPayload
        {
            Command = RoombaCommand.Start,
            Time = _timeProvider.GetTimestampSeconds(),
            Initiator = MessageInitiator,
            Ordered = ordered ? 1 : 0,
            PmapId = pmapId,
            Regions = [.. regions],
            UserPmapvId = userPmapvId,
        };

        _logger.LogInformation(
            "Publishing region clean to '{Topic}' for pmap '{PmapId}' ({Count} region(s))",
            RoombaTopic.Cmd,
            pmapId,
            regions.Count
        );

        var success = await _mqttPublisher.PublishAsync(
            mqttClient,
            RoombaTopic.Cmd,
            payload,
            RoombaJsonContext.Default.RegionCleanPayload,
            cancellationToken
        );

        if (success)
        {
            _logger.LogInformation("Region clean command sent successfully");
        }
        else
        {
            _logger.LogError("Failed to send region clean command");
        }
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
