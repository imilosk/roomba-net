using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Cli.Commands;
using Bootstrapper = RoombaNet.Cli.Bootstrapper;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// The robot profile must be known before the DI container (and its settings) is built,
// so it is pre-parsed out of the raw args here. It is also registered as a global option
// below so it appears in --help and is accepted anywhere on the command line.
var robotProfile = GetRobotProfile(args);

var services = Bootstrapper.CreateServiceProvider(robotProfile);
var commandBuilder = services.GetRequiredService<CliCommandBuilder>();

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

AppDomain.CurrentDomain.ProcessExit += (_, _) => { cancellationTokenSource.Cancel(); };
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent the process from terminating immediately
    cancellationTokenSource.Cancel();
    Console.WriteLine("Cancellation requested. Exiting...");
};

var rootCommand = new RootCommand("RoombaNet CLI");

var robotOption = new Option<string>("--robot", "-r")
{
    Description = "Robot profile to target (e.g. roomba, braava). Mapped via Robots:Profiles in config.",
    Recursive = true,
};
rootCommand.Options.Add(robotOption);

var commands = commandBuilder.BuildCommands(cancellationToken);
foreach (var command in commands)
{
    rootCommand.Subcommands.Add(command);
}

return await rootCommand
    .Parse(args)
    .InvokeAsync();

static string? GetRobotProfile(string[] arguments)
{
    for (var i = 0; i < arguments.Length; i++)
    {
        var arg = arguments[i];
        if (arg is "--robot" or "-r")
        {
            return i + 1 < arguments.Length ? arguments[i + 1] : null;
        }

        if (arg.StartsWith("--robot=", StringComparison.Ordinal))
        {
            return arg["--robot=".Length..];
        }

        if (arg.StartsWith("-r=", StringComparison.Ordinal))
        {
            return arg["-r=".Length..];
        }
    }

    return null;
}