using System.CommandLine;
using System.Text.Json.Nodes;
using RoombaNet.Core;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Cli.Commands;

public class MapsCommand : Command
{
    private readonly IRoombaMapService _mapService;
    private readonly IRoombaSubscriptionService _subscriber;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly CancellationToken _cancellationToken;

    public MapsCommand(
        IRoombaMapService mapService,
        IRoombaSubscriptionService subscriber,
        IRoombaConnectionManager connectionManager,
        CancellationToken cancellationToken = default
    ) : base("maps", "Manage persistent maps")
    {
        _mapService = mapService;
        _subscriber = subscriber;
        _connectionManager = connectionManager;
        _cancellationToken = cancellationToken;

        var listCommand = new Command("list", "List persistent maps stored on the robot");
        listCommand.Aliases.Add("ls");
        listCommand.SetAction(async _ => await ListMaps());
        Subcommands.Add(listCommand);

        var clearCommand = new Command("clear", "Delete ALL persistent maps (local-only). The robot remaps on its next run.");
        clearCommand.Aliases.Add("delete-all");
        clearCommand.SetAction(async _ => await ClearMaps());
        Subcommands.Add(clearCommand);
    }

    private async Task ClearMaps()
    {
        var success = await _mapService.ClearAllMaps(_cancellationToken);
        Console.WriteLine(success
            ? "✅ All persistent maps cleared. The robot will build a fresh map on its next mapping/clean run."
            : "❌ Failed to clear maps.");
    }

    private async Task ListMaps()
    {
        Console.WriteLine("Reading maps from robot...");

        var state = await StateReader.ReadState(
            _subscriber,
            _connectionManager,
            s => s.ContainsKey("pmaps"),
            TimeSpan.FromSeconds(8),
            _cancellationToken);

        if (state["pmaps"] is not JsonArray pmaps || pmaps.Count == 0)
        {
            Console.WriteLine("No persistent maps found on this robot.");
            return;
        }

        Console.WriteLine($"Found {pmaps.Count} persistent map(s):");
        foreach (var entry in pmaps)
        {
            if (entry is not JsonObject mapObject)
            {
                continue;
            }

            foreach (var kv in mapObject)
            {
                Console.WriteLine($"  pmap_id: {kv.Key}   user_pmapv_id: {kv.Value}");
            }
        }
    }
}
