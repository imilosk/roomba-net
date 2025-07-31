using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Cli.Commands;
using Bootstrapper = RoombaNet.Cli.Bootstrapper;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var services = Bootstrapper.CreateServiceProvider();
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
var commands = commandBuilder.BuildCommands(cancellationToken);
foreach (var command in commands)
{
    rootCommand.Subcommands.Add(command);
}

return await rootCommand
    .Parse(args)
    .InvokeAsync();