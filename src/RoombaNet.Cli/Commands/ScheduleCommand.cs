using System.CommandLine;
using System.Text.Json.Nodes;
using RoombaNet.Core;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Cli.Commands;

/// <summary>
/// Manage the robot's weekly cleaning schedule via the legacy <c>schedule</c> delta
/// (<c>{cycle[7],h[7],m[7]}</c>, indexed 0=Sunday..6=Saturday). The schedule is stored on
/// the robot and fires on-device, so it keeps working with no cloud connection.
/// </summary>
public class ScheduleCommand : Command
{
    private static readonly string[] DayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    private readonly IRoombaSettingsService _settings;
    private readonly IRoombaSubscriptionService _subscriber;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly CancellationToken _cancellationToken;

    public ScheduleCommand(
        IRoombaSettingsService settings,
        IRoombaSubscriptionService subscriber,
        IRoombaConnectionManager connectionManager,
        CancellationToken cancellationToken = default
    ) : base("schedule", "View or set the weekly cleaning schedule (local, runs on-robot)")
    {
        _settings = settings;
        _subscriber = subscriber;
        _connectionManager = connectionManager;
        _cancellationToken = cancellationToken;

        var showCommand = new Command("show", "Print the current weekly schedule");
        showCommand.SetAction(async _ => await Show());
        Subcommands.Add(showCommand);

        var daysArg = new Argument<string>("days")
        {
            Description = "Days: comma list (mon,wed,fri) or daily/weekdays/weekends",
        };
        var timeArg = new Argument<string>("time") { Description = "Time of day, HH:MM (24h)" };

        var setCommand = new Command("set", "Schedule a clean on the given days at the given time");
        setCommand.Arguments.Add(daysArg);
        setCommand.Arguments.Add(timeArg);
        setCommand.SetAction(async parse =>
            await Set(parse.GetValue(daysArg)!, parse.GetValue(timeArg)!));
        Subcommands.Add(setCommand);

        var clearDaysArg = new Argument<string>("days")
        {
            Description = "Days to clear, or 'all'",
            DefaultValueFactory = _ => "all",
        };
        var clearCommand = new Command("clear", "Clear the schedule for the given days (default: all)");
        clearCommand.Arguments.Add(clearDaysArg);
        clearCommand.SetAction(async parse => await Clear(parse.GetValue(clearDaysArg)!));
        Subcommands.Add(clearCommand);
    }

    private async Task<(string[] cycle, int[] h, int[] m)> ReadCurrent()
    {
        var state = await StateReader.ReadState(
            _subscriber,
            _connectionManager,
            s => s.ContainsKey("cleanSchedule2") || s.ContainsKey("schedule") || s.ContainsKey("batPct"),
            TimeSpan.FromSeconds(6),
            _cancellationToken);

        var cycle = new string[7];
        Array.Fill(cycle, "none");
        var h = new int[7];
        var m = new int[7];

        // Modern firmware reports cleanSchedule2: an array of events, each with
        // start{day[],hour,min}, enabled, and cmd{command}.
        if (state["cleanSchedule2"] is JsonArray events)
        {
            foreach (var node in events)
            {
                if (node is not JsonObject ev) continue;
                if (ev["enabled"]?.GetValue<bool>() == false) continue;
                var start = ev["start"] as JsonObject;
                if (start?["day"] is not JsonArray days) continue;

                var hour = start["hour"]?.GetValue<int>() ?? 0;
                var min = start["min"]?.GetValue<int>() ?? 0;
                foreach (var d in days)
                {
                    var idx = d?.GetValue<int>() ?? -1;
                    if (idx is >= 0 and < 7)
                    {
                        cycle[idx] = "start";
                        h[idx] = hour;
                        m[idx] = min;
                    }
                }
            }
        }

        return (cycle, h, m);
    }

    private async Task Show()
    {
        var (cycle, h, m) = await ReadCurrent();
        Console.WriteLine("──────── Weekly schedule ────────");
        var any = false;
        for (var i = 0; i < 7; i++)
        {
            if (cycle[i] == "start")
            {
                Console.WriteLine($"  {DayNames[i]}  {h[i]:D2}:{m[i]:D2}  clean");
                any = true;
            }
            else
            {
                Console.WriteLine($"  {DayNames[i]}  --:--  (off)");
            }
        }

        if (!any)
        {
            Console.WriteLine("  (no cleaning days scheduled)");
        }

        Console.WriteLine("─────────────────────────────────");
    }

    private async Task Set(string days, string time)
    {
        var indices = ParseDays(days);
        var (hour, minute) = ParseTime(time);

        var (cycle, h, m) = await ReadCurrent();
        foreach (var i in indices)
        {
            cycle[i] = "start";
            h[i] = hour;
            m[i] = minute;
        }

        await _settings.SetSchedule(cycle, h, m, _cancellationToken);
        Console.WriteLine(
            $"✅ Scheduled clean on {string.Join(", ", indices.Select(i => DayNames[i]))} at {hour:D2}:{minute:D2}");
    }

    private async Task Clear(string days)
    {
        var (cycle, h, m) = await ReadCurrent();
        var indices = days.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? Enumerable.Range(0, 7).ToArray()
            : ParseDays(days);

        foreach (var i in indices)
        {
            cycle[i] = "none";
            h[i] = 0;
            m[i] = 0;
        }

        await _settings.SetSchedule(cycle, h, m, _cancellationToken);
        Console.WriteLine($"✅ Cleared schedule for {(indices.Length == 7 ? "all days" : string.Join(", ", indices.Select(i => DayNames[i])))}");
    }

    private static int[] ParseDays(string days)
    {
        var token = days.Trim().ToLowerInvariant();
        switch (token)
        {
            case "daily" or "everyday" or "all":
                return Enumerable.Range(0, 7).ToArray();
            case "weekdays":
                return [1, 2, 3, 4, 5];
            case "weekends":
                return [0, 6];
        }

        var map = new Dictionary<string, int>
        {
            ["sun"] = 0, ["mon"] = 1, ["tue"] = 2, ["wed"] = 3, ["thu"] = 4, ["fri"] = 5, ["sat"] = 6,
        };

        var result = new List<int>();
        foreach (var part in token.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var key = part.Length > 3 ? part[..3] : part;
            if (!map.TryGetValue(key, out var idx))
            {
                throw new ArgumentException($"Unknown day '{part}'. Use mon,tue,... or daily/weekdays/weekends.");
            }

            if (!result.Contains(idx))
            {
                result.Add(idx);
            }
        }

        if (result.Count == 0)
        {
            throw new ArgumentException("No valid days given.");
        }

        return [.. result];
    }

    private static (int hour, int minute) ParseTime(string time)
    {
        var parts = time.Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var hour)
            || !int.TryParse(parts[1], out var minute)
            || hour is < 0 or > 23
            || minute is < 0 or > 59)
        {
            throw new ArgumentException($"Invalid time '{time}'. Use HH:MM (24h), e.g. 09:30.");
        }

        return (hour, minute);
    }
}
