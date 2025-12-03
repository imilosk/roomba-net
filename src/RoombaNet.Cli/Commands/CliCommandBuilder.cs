using RoombaNet.Core;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriptionService _roombaSubscriber;
    private readonly IRoombaCommandService _roombaCommandClient;
    private readonly IRoombaSettingsService _roombaSettingsClient;
    private readonly IRoombaWifiService _roombaWifiClient;

    public CliCommandBuilder(
        IRoombaSubscriptionService roombaSubscriber,
        IRoombaCommandService roombaCommandClient,
        IRoombaSettingsService roombaSettingsClient,
        IRoombaWifiService roombaWifiClient
    )
    {
        _roombaSubscriber = roombaSubscriber;
        _roombaCommandClient = roombaCommandClient;
        _roombaSettingsClient = roombaSettingsClient;
        _roombaWifiClient = roombaWifiClient;
    }

    public IEnumerable<Command> BuildCommands(CancellationToken cancellationToken)
    {
        return
        [
            new SubscribeCommand(_roombaSubscriber, cancellationToken),
            new ExecuteCommand(_roombaCommandClient, cancellationToken),
            new SettingCommand(_roombaSettingsClient, cancellationToken),
            new GetCommand(_roombaSettingsClient, cancellationToken),
            new DiscoverCommand(_roombaSettingsClient, cancellationToken),
            new ConfigureWifiCommand(_roombaWifiClient, cancellationToken)
        ];
    }
}