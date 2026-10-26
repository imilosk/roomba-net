using System.CommandLine;
using RoombaNet.Core;
using RoombaNet.Core.Constants;

namespace RoombaNet.Cli.Commands;

public class SettingCommand : Command
{
    private readonly IRoombaSettingsService _roombaSettingsClient;
    private readonly CancellationToken _cancellationToken;

    public SettingCommand(
        IRoombaSettingsService roombaSettingsClient,
        CancellationToken cancellationToken = default
    ) : base("setting", "Manage Roomba settings")
    {
        _roombaSettingsClient = roombaSettingsClient;
        _cancellationToken = cancellationToken;

        AddBooleanSubcommand("childlock", "Child/Pet Lock", SetChildLock);
        AddBooleanSubcommand("binpause", "Bin Pause", SetBinPause);
        AddBooleanSubcommand("edgeclean", "Edge/corner cleaning", SetEdgeClean, "edge");
        AddEnumSubcommand<RoombaCleaningPasses>(
            "cleaningpasses",
            "Cleaning Passes",
            SetCleaningPasses,
            "pass"
        );
        AddEnumSubcommand<RoombaCleaningPower>(
            "cleaningpower",
            "Vacuum suction power",
            SetCleaningPower,
            "power"
        );
        AddNameSubcommand();

        AddBooleanSubcommand("ecocharge", "Battery-longevity eco charging", SetEcoCharge);
        AddBooleanSubcommand("mapupload", "Allow uploading the learned map to the cloud", SetMapUpload);
        AddBooleanSubcommand("maplearning", "Allow persistent-map learning", SetMapLearning);
        AddBooleanSubcommand(
            "continuouslearning",
            "Allow continuous persistent-map learning",
            SetContinuousMapLearning,
            "cl");
        AddBooleanSubcommand("perimeter", "Perimeter/edge pass (inverse of noPP)", SetPerimeterPass, "perimeterpass");
        AddIntSubcommand("bintypedetect", "Bin-type auto-detection", SetBinTypeDetect, "value", "bintype");
        AddIntSubcommand("volume", "Speaker volume (0-100)", SetVolume, "level");
        AddStringSubcommand("timezone", "IANA timezone, e.g. Europe/Ljubljana", SetTimezone, "iana", "tz");
        AddStringSubcommand("language", "Voice language code, e.g. en-US_1", SetLanguage, "code", "lang");

        Aliases.Add("set");
    }

    private void AddNameSubcommand()
    {
        var command = new Command("name", "Rename the robot");
        var nameArgument = new Argument<string>("value") { Description = "The new robot name" };
        command.Arguments.Add(nameArgument);
        command.SetAction(async parseResult =>
            await _roombaSettingsClient.SetName(parseResult.GetValue(nameArgument)!, _cancellationToken));
        Subcommands.Add(command);
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

    private void AddEnumSubcommand<TEnum>(
        string commandName,
        string description,
        Func<TEnum, Task> handler,
        params string[] aliases
    ) where TEnum : struct, Enum
    {
        var command = new Command(commandName, description);

        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        foreach (var value in Enum.GetValues<TEnum>())
        {
            var valueCommand = new Command(value.ToString().ToLowerInvariant(), $"Set to {value}");
            valueCommand.Aliases.Add(Convert.ToInt32(value).ToString());

            valueCommand.SetAction(async _ => await handler(value));

            command.Subcommands.Add(valueCommand);
        }

        Subcommands.Add(command);
    }

    private void AddIntSubcommand(
        string commandName,
        string description,
        Func<int, Task> handler,
        string argumentName,
        params string[] aliases
    )
    {
        var command = new Command(commandName, description);
        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        var argument = new Argument<int>(argumentName) { Description = description };
        command.Arguments.Add(argument);
        command.SetAction(async parseResult => await handler(parseResult.GetValue(argument)));
        Subcommands.Add(command);
    }

    private void AddStringSubcommand(
        string commandName,
        string description,
        Func<string, Task> handler,
        string argumentName,
        params string[] aliases
    )
    {
        var command = new Command(commandName, description);
        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        var argument = new Argument<string>(argumentName) { Description = description };
        command.Arguments.Add(argument);
        command.SetAction(async parseResult => await handler(parseResult.GetValue(argument)!));
        Subcommands.Add(command);
    }

    private async Task SetChildLock(bool enable) =>
        await _roombaSettingsClient.SetChildLock(enable, _cancellationToken);

    private async Task SetBinPause(bool enable) => await _roombaSettingsClient.SetBinPause(enable, _cancellationToken);

    private async Task SetCleaningPasses(RoombaCleaningPasses passes) =>
        await _roombaSettingsClient.CleaningPasses(passes, _cancellationToken);

    private async Task SetEdgeClean(bool enable) =>
        await _roombaSettingsClient.SetEdgeClean(enable, _cancellationToken);

    private async Task SetCleaningPower(RoombaCleaningPower power) =>
        await _roombaSettingsClient.SetCleaningPower(power, _cancellationToken);

    private async Task SetEcoCharge(bool enable) =>
        await _roombaSettingsClient.SetEcoCharge(enable, _cancellationToken);

    private async Task SetMapUpload(bool enable) =>
        await _roombaSettingsClient.SetMapUpload(enable, _cancellationToken);

    private async Task SetMapLearning(bool enable) =>
        await _roombaSettingsClient.SetMapLearning(enable, _cancellationToken);

    private async Task SetContinuousMapLearning(bool enable) =>
        await _roombaSettingsClient.SetContinuousMapLearning(enable, _cancellationToken);

    private async Task SetPerimeterPass(bool enable) =>
        await _roombaSettingsClient.SetPerimeterPass(enable, _cancellationToken);

    private async Task SetBinTypeDetect(int value) =>
        await _roombaSettingsClient.SetBinTypeDetect(value, _cancellationToken);

    private async Task SetVolume(int level) =>
        await _roombaSettingsClient.SetVolume(level, _cancellationToken);

    private async Task SetTimezone(string iana) =>
        await _roombaSettingsClient.SetTimezone(iana, _cancellationToken);

    private async Task SetLanguage(string code) =>
        await _roombaSettingsClient.SetLanguage(code, _cancellationToken);
}
