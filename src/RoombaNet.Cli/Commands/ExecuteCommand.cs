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
    ) : base("execute", "Manage Roomba operations")
    {
        _roombaClient = roombaClient;
        _cancellationToken = cancellationToken;

        AddSubcommand("find", "Make Roomba beep to locate it", ExecuteFind);
        AddSubcommand("start", "Start cleaning", ExecuteStart);
        AddSubcommand("stop", "Stop cleaning", ExecuteStop);
        AddSubcommand("pause", "Pause cleaning", ExecutePause);
        AddSubcommand("resume", "Resume cleaning", ExecuteResume);
        AddSubcommand("dock", "Return to dock", ExecuteDock);
        AddSubcommand("evac", "Empty the bin", ExecuteEvac);
    }

    private void AddSubcommand(string commandName, string description, Func<Task> handler)
    {
        var command = new Command(commandName, description);
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
}