using RoombaNet.Core;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriber _roombaSubscriber;
    private readonly IRoombaCommandClient _roombaCommandClient;
    private readonly IRoombaSettingsClient _roombaSettingsClient;
    private readonly IRoombaWifiClient _roombaWifiClient;

    public CliCommandBuilder(
        IRoombaSubscriber roombaSubscriber,
        IRoombaCommandClient roombaCommandClient,
        IRoombaSettingsClient roombaSettingsClient,
        IRoombaWifiClient roombaWifiClient
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