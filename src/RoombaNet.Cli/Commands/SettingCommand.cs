using System.CommandLine;
using RoombaNet.Mqtt;

namespace RoombaNet.Cli.Commands;

public class SettingCommand : Command
{
    private readonly IRoombaClient _roombaClient;
    private readonly CancellationToken _cancellationToken;

    public SettingCommand(
        IRoombaClient roombaClient,
        CancellationToken cancellationToken = default
    ) : base("setting", "Manage Roomba settings")
    {
        _roombaClient = roombaClient;
        _cancellationToken = cancellationToken;

        AddSubcommand("childlock", "Train the Roomba / Mapping run", SetChildLock);
    }

    private void AddSubcommand(string commandName, string description, Func<Task> handler, params string[] aliases)
    {
        var command = new Command(commandName, description);

        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        command.SetAction(async _ => await handler());

        Subcommands.Add(command);
    }

    private async Task SetChildLock() => await _roombaClient.ChildLock(true, _cancellationToken);
}
