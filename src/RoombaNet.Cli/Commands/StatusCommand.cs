using System.CommandLine;
using System.Text.Json.Nodes;
using RoombaNet.Core;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Cli.Commands;

public class StatusCommand : Command
{
    private readonly IRoombaSubscriptionService _subscriber;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly CancellationToken _cancellationToken;

    public StatusCommand(
        IRoombaSubscriptionService subscriber,
        IRoombaConnectionManager connectionManager,
        CancellationToken cancellationToken = default
    ) : base("status", "Print a one-shot status snapshot and exit")
    {
        _subscriber = subscriber;
        _connectionManager = connectionManager;
        _cancellationToken = cancellationToken;
        Aliases.Add("state");
        SetAction(async _ => await PrintStatus());
    }

    private async Task PrintStatus()
    {
        Console.WriteLine("Reading status from robot...");

        var state = await StateReader.ReadState(
            _subscriber,
            _connectionManager,
            s => s.ContainsKey("batPct") && s.ContainsKey("bin"),
            TimeSpan.FromSeconds(6),
            _cancellationToken);

        if (state.Count == 0)
        {
            Console.WriteLine("No status received (robot may be asleep or unreachable).");
            return;
        }

        var mission = state["cleanMissionStatus"] as JsonObject;

        Console.WriteLine("──────────── Roomba Status ────────────");
        Console.WriteLine($"  {"Activity",-16}: {DescribeActivity(mission, state["batPct"])}");
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

    private static string DescribeActivity(JsonObject? mission, JsonNode? battery)
    {
        var phase = mission?["phase"]?.GetValue<string>();
        var cycle = mission?["cycle"]?.GetValue<string>();
        var error = mission?["error"]?.GetValue<int>();
        var notReady = mission?["notReady"]?.GetValue<int>();
        var bat = battery?.GetValue<int>();

        if (error is not (null or 0)) return $"Error (code {error})";
        if (notReady is not (null or 0)) return $"Not ready (code {notReady})";

        var missionActive = cycle is "clean" or "quick" or "spot";
        return phase switch
        {
            "run" when cycle == "evac" => "Emptying bin",
            "run" => "Cleaning",
            "evac" => "Emptying bin",
            "stuck" => "Stuck — needs help",
            "hmMidMsn" => "Returning to dock to recharge (will resume)",
            "hmUsrDock" => "Returning to dock (requested)",
            "hmPostMsn" => "Returning to dock (mission complete)",
            "charge" when missionActive => "Docked, charging — will resume mission",
            "charge" when bat is >= 100 => "Docked — fully charged, idle",
            "charge" => "Docked and charging",
            "stop" when missionActive => "Paused",
            "stop" => "Idle (stopped)",
            "pause" => "Paused",
            null => "Unknown",
            _ => phase,
        };
    }
}

