using RoombaNet.Mqtt;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriber _roombaSubscriber;
    private readonly IRoombaCommandClient _roombaCommandClient;
    private readonly IRoombaSettingsClient _roombaSettingsClient;

    public CliCommandBuilder(
        IRoombaSubscriber roombaSubscriber,
        IRoombaCommandClient roombaCommandClient,
        IRoombaSettingsClient roombaSettingsClient
    )
    {
        _roombaSubscriber = roombaSubscriber;
        _roombaCommandClient = roombaCommandClient;
        _roombaSettingsClient = roombaSettingsClient;
    }

    public IEnumerable<Command> BuildCommands(CancellationToken cancellationToken)
    {
        return
        [
            new SubscribeCommand(_roombaSubscriber, cancellationToken),
            new ExecuteCommand(_roombaCommandClient, cancellationToken),
            new SettingCommand(_roombaSettingsClient, cancellationToken),
        ];
    }
}