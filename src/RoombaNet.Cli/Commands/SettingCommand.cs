using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class SettingCommand : Command
{
    private readonly IRoombaSettingsClient _roombaSettingsClient;
    private readonly CancellationToken _cancellationToken;

    public SettingCommand(
        IRoombaSettingsClient roombaSettingsClient,
        CancellationToken cancellationToken = default
    ) : base("setting", "Manage Roomba settings")
    {
        _roombaSettingsClient = roombaSettingsClient;
        _cancellationToken = cancellationToken;

        AddBooleanSubcommand("childlock", "Child/Pet Lock", SetChildLock);
        AddBooleanSubcommand("binpause", "Bin Pause", SetBinPause);
    }

    private void AddBooleanSubcommand(
        string commandName,
        string description,
        Func<bool, Task> handler,
        params string[] aliases
    )
    {
        var command = new Command(commandName, description);

        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        var enableCommand = new Command("enable", "Enable the setting");
        enableCommand.Aliases.Add("on");
        enableCommand.Aliases.Add("true");
        enableCommand.Aliases.Add("yes");
        enableCommand.Aliases.Add("1");
        enableCommand.SetAction(async _ => await handler(true));
        command.Subcommands.Add(enableCommand);

        var disableCommand = new Command("disable", "Disable the setting");
        disableCommand.Aliases.Add("off");
        disableCommand.Aliases.Add("false");
        disableCommand.Aliases.Add("no");
        disableCommand.Aliases.Add("0");
        disableCommand.SetAction(async _ => await handler(false));
        command.Subcommands.Add(disableCommand);

        Subcommands.Add(command);
    }

    private async Task SetChildLock(bool enable) => await _roombaSettingsClient.ChildLock(enable, _cancellationToken);
    private async Task SetBinPause(bool enable) => await _roombaSettingsClient.BinPause(enable, _cancellationToken);
}
