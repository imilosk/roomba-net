using System.CommandLine;
using RoombaNet.Proxy;

namespace RoombaNet.Cli.Commands;

/// <summary>
/// Runs the multi-robot MQTT proxy in the foreground. The proxy holds the single upstream connection
/// to each configured robot (from <c>Robots:Profiles</c> or <c>Proxy:Robots</c>) and multiplexes it to
/// many clients over a native drop-in TLS listener and an optional plain localhost listener.
/// </summary>
public class ProxyCommand : Command
{
    private readonly RoombaProxyService _proxyService;
    private readonly CancellationToken _cancellationToken;

    public ProxyCommand(
        RoombaProxyService proxyService,
        CancellationToken cancellationToken = default
    ) : base("proxy", "Run the multi-robot MQTT proxy (hold one upstream connection per robot, fan out to many clients)")
    {
        _proxyService = proxyService;
        _cancellationToken = cancellationToken;
        SetAction(ExecuteCommand);
    }

    private async Task ExecuteCommand(ParseResult parseResult)
    {
        await _proxyService.StartAsync(_cancellationToken);
        Console.WriteLine("Proxy running. Press Ctrl+C to stop.");

        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on Ctrl+C
        }
        finally
        {
            await _proxyService.StopAsync(CancellationToken.None);
        }
    }
}
