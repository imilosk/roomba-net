using System.CommandLine;
using System.Text.Json.Nodes;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class StatusCommand : Command
{
    private readonly IRoombaSubscriptionService _subscriber;
    private readonly CancellationToken _cancellationToken;

    public StatusCommand(
        IRoombaSubscriptionService subscriber,
        CancellationToken cancellationToken = default
    ) : base("status", "Print a one-shot status snapshot and exit")
    {
        _subscriber = subscriber;
        _cancellationToken = cancellationToken;
        Aliases.Add("state");
        SetAction(async _ => await PrintStatus());
    }

    private async Task PrintStatus()
    {
        Console.WriteLine("Reading status from robot...");

        var state = await StateReader.ReadState(
            _subscriber,
            s => s.ContainsKey("batPct") && s.ContainsKey("cleanMissionStatus"),
            TimeSpan.FromSeconds(6),
            _cancellationToken);

        if (state.Count == 0)
        {
            Console.WriteLine("No status received (robot may be asleep or unreachable).");
            return;
        }

        var mission = state["cleanMissionStatus"] as JsonObject;

        Console.WriteLine("──────────── Roomba Status ────────────");
        PrintField("Name", state["name"]);
        PrintField("SKU", state["sku"]);
        PrintField("Battery %", state["batPct"]);
        PrintField("Phase", mission?["phase"]);
        PrintField("Cycle", mission?["cycle"]);
        PrintField("Mission error", mission?["error"]);
        PrintField("Bin present", (state["bin"] as JsonObject)?["present"]);
        PrintField("Bin full", (state["bin"] as JsonObject)?["full"]);
        var pose = state["pose"] as JsonObject;
        if (pose is not null)
        {
            var point = pose["point"] as JsonObject;
            Console.WriteLine($"  {"Position",-16}: x={point?["x"]}, y={point?["y"]}, θ={pose["theta"]}");
        }

        var pmaps = state["pmaps"] as JsonArray;
        PrintField("Persistent maps", pmaps?.Count);
        Console.WriteLine("───────────────────────────────────────");
    }

    private static void PrintField(string label, JsonNode? value)
    {
        if (value is not null)
        {
            Console.WriteLine($"  {label,-16}: {value}");
        }
    }
}
