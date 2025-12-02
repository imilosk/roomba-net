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

        var passwordCommand = new Command("password", "Get Roomba password (hold HOME button for 2 seconds first)");
        passwordCommand.Aliases.Add("pwd");
        passwordCommand.Aliases.Add("pass");

        var ipOption = new Option<string>("--ip")
        {
            Description = "Roomba IP address",
        };
        var portOption = new Option<int>("--port")
        {
            Description = "Roomba port",
            DefaultValueFactory = _ => 8883,
        };

        passwordCommand.Add(ipOption);
        passwordCommand.Add(portOption);

        passwordCommand.SetAction(async parseResult =>
        {
            var ip = parseResult.GetValue(ipOption);
            if (string.IsNullOrEmpty(ip))
            {
                Console.WriteLine("Error: --ip parameter is required");
                return;
            }

            var port = parseResult.GetValue(portOption);
            await GetPassword(ip, port);
        });

        Subcommands.Add(passwordCommand);
    }

    private async Task GetPassword(string ipAddress, int port)
    {
        try
        {
            var password = await _roombaSettingsClient.GetPassword(ipAddress, port, _cancellationToken);
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Failed to retrieve password. See README for instructions.");
                return;
            }

            Console.WriteLine($"Password=> {password} <= Yes, all this string.");
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to retrieve password. See README for instructions.");
        }
    }
}
