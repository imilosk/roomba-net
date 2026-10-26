using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class ExecuteCommand : Command
{
    private readonly IRoombaCommandService _roombaCommandClient;
    private readonly CancellationToken _cancellationToken;

    public ExecuteCommand(
        IRoombaCommandService roombaClient,
        CancellationToken cancellationToken
    ) : base("command", "Manage Roomba operations")
    {
        _roombaCommandClient = roombaClient;
        _cancellationToken = cancellationToken;

        Aliases.Add("cmd");

        AddSubcommand("find", "Make Roomba beep to locate it", ExecuteFind, "locate");
        AddSubcommand("start", "Start cleaning", ExecuteStart, "clean");
        AddSubcommand("stop", "Stop cleaning", ExecuteStop, "halt");
        AddSubcommand("pause", "Pause cleaning", ExecutePause);
        AddSubcommand("resume", "Resume cleaning", ExecuteResume, "continue");
        AddSubcommand("dock", "Return to dock", ExecuteDock, "home");
        AddSubcommand("evac", "Empty the bin", ExecuteEvac, "empty");
        AddSubcommand("reset", "Reset the Roomba", ExecuteReset, "reboot");
        AddSubcommand("train", "Train the Roomba (Mapping run)", ExecuteTrain, "map");
        AddSubcommand("spot", "Spot clean", ExecuteSpot);
        AddSubcommand("wake", "Wake the Roomba", ExecuteWake);
        AddSubcommand("sleep", "Put the Roomba to sleep", ExecuteSleep);
        AddSubcommand("off", "Power off the Roomba", ExecuteOff);
        AddSubcommand("tidy", "Tidy-up clean", ExecuteTidy);
        AddSubcommand("skip", "Skip the current area/room", ExecuteSkip);
        AddSubcommand("stopevac", "Stop bin evacuation", ExecuteStopEvac);
        AddSubcommand("cancel", "Cancel the mission (stop + dock) so it won't auto-resume", ExecuteCancel);
        AddSubcommand("wifi-on", "Turn the Roomba Wi-Fi radio on", ExecuteWifiOn, "wlapon");
        AddSubcommand("wifi-off", "Turn the Roomba Wi-Fi radio off", ExecuteWifiOff, "wlapoff");

        // Braava dock/pad maintenance commands (first-class wrappers over the raw cmd strings).
        AddRawSubcommand("washpad", "Braava: wash the cleaning pad");
        AddRawSubcommand("stoppadwash", "Braava: stop washing the pad");
        AddRawSubcommand("drypad", "Braava: dry the cleaning pad");
        AddRawSubcommand("stoppaddry", "Braava: stop drying the pad");
        AddRawSubcommand("flushsluice", "Braava: flush the water system");
        AddRawSubcommand("flrefill", "Braava: refill the liquid tank");
        AddRawSubcommand("stopflrefill", "Braava: stop refilling the tank");
        AddRawSubcommand("spray", "Braava: activate the spray jet");
        AddRawSubcommand("startvibrate", "Braava: start the vibration motor");
        AddRawSubcommand("stopvibrate", "Braava: stop the vibration motor");

        var rawCommand = new Command("raw", "Send an arbitrary command string to the cmd topic");
        var commandArgument = new Argument<string>("command") { Description = "The raw command to send" };
        rawCommand.Arguments.Add(commandArgument);
        rawCommand.SetAction(async parseResult =>
            await _roombaCommandClient.Send(parseResult.GetValue(commandArgument)!, _cancellationToken));
        Subcommands.Add(rawCommand);
    }

    private void AddRawSubcommand(string commandName, string description, params string[] aliases) =>
        AddSubcommand(commandName, description, () => _roombaCommandClient.Send(commandName, _cancellationToken), aliases);

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

    private async Task ExecuteFind() => await _roombaCommandClient.Find(_cancellationToken);
    private async Task ExecuteStart() => await _roombaCommandClient.Start(_cancellationToken);
    private async Task ExecuteStop() => await _roombaCommandClient.Stop(_cancellationToken);
    private async Task ExecutePause() => await _roombaCommandClient.Pause(_cancellationToken);
    private async Task ExecuteResume() => await _roombaCommandClient.Resume(_cancellationToken);
    private async Task ExecuteDock() => await _roombaCommandClient.Dock(_cancellationToken);
    private async Task ExecuteEvac() => await _roombaCommandClient.Evac(_cancellationToken);
    private async Task ExecuteReset() => await _roombaCommandClient.Reset(_cancellationToken);
    private async Task ExecuteTrain() => await _roombaCommandClient.Train(_cancellationToken);
    private async Task ExecuteSpot() => await _roombaCommandClient.Spot(_cancellationToken);
    private async Task ExecuteWake() => await _roombaCommandClient.Wake(_cancellationToken);
    private async Task ExecuteSleep() => await _roombaCommandClient.Sleep(_cancellationToken);
    private async Task ExecuteOff() => await _roombaCommandClient.Off(_cancellationToken);
    private async Task ExecuteTidy() => await _roombaCommandClient.Tidy(_cancellationToken);
    private async Task ExecuteSkip() => await _roombaCommandClient.Skip(_cancellationToken);
    private async Task ExecuteStopEvac() => await _roombaCommandClient.StopEvac(_cancellationToken);
    private async Task ExecuteWifiOn() => await _roombaCommandClient.WifiOn(_cancellationToken);
    private async Task ExecuteWifiOff() => await _roombaCommandClient.WifiOff(_cancellationToken);

    private async Task ExecuteCancel()
    {
        // stop ends the mission (cycle -> none), cancelling any pending recharge-and-resume.
        await _roombaCommandClient.Stop(_cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(2), _cancellationToken);
        await _roombaCommandClient.Dock(_cancellationToken);
    }
}