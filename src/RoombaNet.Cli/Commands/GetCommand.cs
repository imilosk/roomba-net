using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class GetCommand : Command
{
    private readonly IRoombaSettingsClient _roombaSettingsClient;
    private readonly CancellationToken _cancellationToken;

    public GetCommand(
        IRoombaSettingsClient roombaSettingsClient,
        CancellationToken cancellationToken = default
    ) : base("get", "Get the Roomba settings")
    {
        _roombaSettingsClient = roombaSettingsClient;
        _cancellationToken = cancellationToken;

        AddSubcommand("password", "Get Roomba password", GetPassword, "pwd", "pass");
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

    private async Task GetPassword()
    {
        var password = await _roombaSettingsClient.GetPassword(_cancellationToken);
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("No password found or unable to retrieve it.");
            return;
        }

        Console.WriteLine($"Password=> {password} <= Yes, all this string.");
    }
}
