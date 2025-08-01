using System.CommandLine;
using RoombaNet.Cli.Services;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class SubscribeCommand : Command
{
    private readonly IRoombaSubscriber _roombaSubscriber;
    private readonly CancellationToken _cancellationToken;

    public SubscribeCommand(
        IRoombaSubscriber roombaSubscriber,
        CancellationToken cancellationToken = default
    ) : base("subscribe", "Subscribe to all Roomba events")
    {
        _roombaSubscriber = roombaSubscriber;
        _cancellationToken = cancellationToken;
        SetAction(ExecuteCommand);
    }

    private async Task ExecuteCommand(ParseResult parseResult)
    {
        await _roombaSubscriber.Subscribe(
            OutputService.PrintMessage,
            _cancellationToken
        );

        while (!_cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
        }
    }
}
