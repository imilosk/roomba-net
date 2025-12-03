using RoombaNet.Core;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriptionService _roombaSubscriber;
    private readonly IRoombaCommandService _roombaCommandClient;
    private readonly IRoombaSettingsService _roombaSettingsClient;
    private readonly IRoombaWifiService _roombaWifiClient;
    private readonly IRoombaPasswordService _roombaPasswordService;
    private readonly IRoombaDiscoveryService _roombaDiscoveryService;

    public CliCommandBuilder(
        IRoombaSubscriptionService roombaSubscriber,
        IRoombaCommandService roombaCommandClient,
        IRoombaSettingsService roombaSettingsClient,
        IRoombaWifiService roombaWifiClient,
        IRoombaPasswordService roombaPasswordService,
        IRoombaDiscoveryService roombaDiscoveryService
    )
    {
        _roombaSubscriber = roombaSubscriber;
        _roombaCommandClient = roombaCommandClient;
        _roombaSettingsClient = roombaSettingsClient;
        _roombaWifiClient = roombaWifiClient;
        _roombaPasswordService = roombaPasswordService;
        _roombaDiscoveryService = roombaDiscoveryService;
    }

    public IEnumerable<Command> BuildCommands(CancellationToken cancellationToken)
    {
        return
        [
            new SubscribeCommand(_roombaSubscriber, cancellationToken),
            new ExecuteCommand(_roombaCommandClient, cancellationToken),
            new SettingCommand(_roombaSettingsClient, cancellationToken),
            new GetCommand(_roombaPasswordService, cancellationToken),
            new DiscoverCommand(_roombaDiscoveryService, cancellationToken),
            new ConfigureWifiCommand(_roombaWifiClient, cancellationToken)
        ];
    }
}