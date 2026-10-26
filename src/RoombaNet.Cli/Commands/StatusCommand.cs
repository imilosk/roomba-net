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
            // Vacuums report a bin, Braavas a water tank instead. Vacuums also push Wi-Fi
            // "signal" on the wifistat topic shortly after; wait for it so the snapshot
            // includes it. Braavas never report signal, so don't block on it for them.
            s => s.ContainsKey("batPct")
                 && (s.ContainsKey("bin") || s.ContainsKey("tankPresent"))
                 && (!s.ContainsKey("bin") || s.ContainsKey("signal")),
            TimeSpan.FromSeconds(8),
            _cancellationToken);

        if (state.Count == 0)
        {
            Console.WriteLine("No status received (robot may be asleep or unreachable).");
            return;
        }

        var mission = state["cleanMissionStatus"] as JsonObject;
        var bin = state["bin"] as JsonObject;

        Console.WriteLine("──────────── Roomba Status ────────────");
        Console.WriteLine($"  {"Activity",-16}: {DescribeActivity(state, mission, bin)}");
        PrintField("Name", state["name"]);
        PrintField("SKU", state["sku"]);
        PrintField("Firmware", state["softwareVer"]);
        PrintField("Battery %", state["batPct"]);
        PrintField("Phase", mission?["phase"]);
        PrintField("Cycle", mission?["cycle"]);
        var missionError = GetInt(mission?["error"]);
        Console.WriteLine($"  {"Mission error",-16}: " +
            (missionError is null or 0
                ? "none"
                : $"{RoombaStatusText.DescribeError(missionError.Value)} (code {missionError})"));
        PrintField("Bin present", bin?["present"]);
        PrintField("Bin full", bin?["full"]);
        PrintMoppingFields(state);
        var pose = state["pose"] as JsonObject;
        if (pose is not null)
        {
            var point = pose["point"] as JsonObject;
            Console.WriteLine($"  {"Position",-16}: x={point?["x"]}, y={point?["y"]}, θ={pose["theta"]}");
        }

        var signal = state["signal"] as JsonObject;
        if (signal is not null)
        {
            var rssi = GetInt(signal["rssi"]);
            var snr = GetInt(signal["snr"]);
            Console.WriteLine($"  {"Wi-Fi signal",-16}: {rssi} dBm (SNR {snr}){DescribeRssi(rssi)}");
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

    private static void PrintMoppingFields(JsonObject state)
    {
        if (!state.ContainsKey("tankPresent"))
        {
            return;
        }

        var detectedPad = GetString(state["detectedPad"]);
        Console.WriteLine($"  {"Pad",-16}: {RoombaStatusText.DescribePad(detectedPad)}");
        PrintField("Tank present", state["tankPresent"]);
        PrintField("Tank level %", state["tankLvl"]);
        PrintField("Lid open", state["lidOpen"]);

        var wetness = state["padWetness"] as JsonObject;
        if (wetness is not null)
        {
            Console.WriteLine(
                $"  {"Liquid amount",-16}: disposable {RoombaStatusText.DescribeLiquidAmount(GetInt(wetness["disposable"]))}, " +
                $"reusable {RoombaStatusText.DescribeLiquidAmount(GetInt(wetness["reusable"]))}");
        }

        var overlap = GetInt(state["rankOverlap"]);
        if (overlap is not null)
        {
            Console.WriteLine($"  {"Mop overlap",-16}: {overlap}%");
        }
    }

    private static string DescribeActivity(JsonObject state, JsonObject? mission, JsonObject? bin)
    {
        var notReady = GetInt(mission?["notReady"]);
        var notReadyCause = notReady is not (null or 0)
            ? RoombaStatusText.DescribeNotReadyCause(
                GetString(state["detectedPad"]),
                GetBool(state["tankPresent"]),
                GetInt(state["tankLvl"]),
                GetBool(state["lidOpen"]),
                GetBool(bin?["present"]))
            : null;

        return RoombaStatusText.DescribeActivity(
            GetString(mission?["phase"]),
            GetString(mission?["cycle"]),
            GetInt(mission?["error"]),
            notReady,
            GetInt(state["batPct"]),
            notReadyCause);
    }

    private static string DescribeRssi(int? rssi) => rssi switch
    {
        null => "",
        >= -50 => " — excellent",
        >= -60 => " — good",
        >= -67 => " — fair",
        >= -75 => " — weak",
        _ => " — very weak",
    };

    private static string? GetString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    private static int? GetInt(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;

    private static bool? GetBool(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<bool>(out var result) ? result : null;
}
