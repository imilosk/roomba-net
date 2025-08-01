using System.CommandLine;
using RoombaNet.Mqtt;

namespace RoombaNet.Cli.Commands;

public class ExecuteCommand : Command
{
    private readonly IRoombaClient _roombaClient;
    private readonly CancellationToken _cancellationToken;

    public ExecuteCommand(
        IRoombaClient roombaClient,
        CancellationToken cancellationToken
    ) : base("command", "Manage Roomba operations")
    {
        _roombaClient = roombaClient;
        _cancellationToken = cancellationToken;

        AddSubcommand("find", "Make Roomba beep to locate it", ExecuteFind, "locate");
        AddSubcommand("start", "Start cleaning", ExecuteStart, "clean");
        AddSubcommand("stop", "Stop cleaning", ExecuteStop, "halt");
        AddSubcommand("pause", "Pause cleaning", ExecutePause);
        AddSubcommand("resume", "Resume cleaning", ExecuteResume, "continue");
        AddSubcommand("dock", "Return to dock", ExecuteDock, "home");
        AddSubcommand("evac", "Empty the bin", ExecuteEvac, "empty");
        AddSubcommand("reset", "Reset the Roomba", ExecuteReset, "reboot");
        AddSubcommand("train", "Train the Roomba / Mapping run", ExecuteTrain, "map");
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

    private async Task ExecuteFind() => await _roombaClient.Find(_cancellationToken);
    private async Task ExecuteStart() => await _roombaClient.Start(_cancellationToken);
    private async Task ExecuteStop() => await _roombaClient.Stop(_cancellationToken);
    private async Task ExecutePause() => await _roombaClient.Pause(_cancellationToken);
    private async Task ExecuteResume() => await _roombaClient.Resume(_cancellationToken);
    private async Task ExecuteDock() => await _roombaClient.Dock(_cancellationToken);
    private async Task ExecuteEvac() => await _roombaClient.Evac(_cancellationToken);
    private async Task ExecuteReset() => await _roombaClient.Reset(_cancellationToken);
    private async Task ExecuteTrain() => await _roombaClient.Train(_cancellationToken);
}