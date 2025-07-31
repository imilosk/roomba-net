using RoombaNet.Mqtt;
using Command = System.CommandLine.Command;


namespace RoombaNet.Cli.Commands;

public class CliCommandBuilder
{
    private readonly IRoombaSubscriber _roombaSubscriber;

    public CliCommandBuilder(IRoombaSubscriber roombaSubscriber)
    {
        _roombaSubscriber = roombaSubscriber;
    }

    public IEnumerable<Command> BuildCommands(CancellationToken cancellationToken)
    {
        return new List<Command>
        {
            new SubscribeCommand(_roombaSubscriber, cancellationToken),
        };
    }
}