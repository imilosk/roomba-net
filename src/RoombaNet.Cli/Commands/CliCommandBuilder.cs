using RoombaNet.Mqtt;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriber _roombaSubscriber;
    private readonly IRoombaClient _roombaClient;

    public CliCommandBuilder(
        IRoombaSubscriber roombaSubscriber,
        IRoombaClient roombaClient
    )
    {
        _roombaSubscriber = roombaSubscriber;
        _roombaClient = roombaClient;
    }

    public IEnumerable<Command> BuildCommands(CancellationToken cancellationToken)
    {
        return new List<Command>
        {
            new SubscribeCommand(_roombaSubscriber, cancellationToken),
            new ExecuteCommand(_roombaClient, cancellationToken),
        };
    }
}