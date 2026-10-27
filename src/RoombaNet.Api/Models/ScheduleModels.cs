namespace RoombaNet.Api.Models;

/// <summary>A single scheduled clean, interpreted from the robot's <c>cleanSchedule2</c>.</summary>
public record ScheduledClean(
    int DayIndex,
    string Day,
    string Time,
    bool Enabled
);

/// <summary>The robot's weekly schedule, parsed into one row per scheduled day.</summary>
public record WeeklyScheduleResponse(
    string RobotId,
    List<ScheduledClean> Cleans
)
{
    private static readonly string[] DayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public static WeeklyScheduleResponse From(string robotId, RoombaState state)
    {
        var cleans = new List<ScheduledClean>();

        foreach (var entry in state.CleanSchedule2 ?? [])
        {
            var days = entry.Start?.Day;
            if (days is null)
            {
                continue;
            }

            var hour = entry.Start?.Hour ?? 0;
            var min = entry.Start?.Min ?? 0;
            var enabled = entry.Enabled ?? true;

            foreach (var dayIndex in days)
            {
                if (dayIndex is < 0 or > 6)
                {
                    continue;
                }

                cleans.Add(new ScheduledClean(
                    DayIndex: dayIndex,
                    Day: DayNames[dayIndex],
                    Time: $"{hour:D2}:{min:D2}",
                    Enabled: enabled));
            }
        }

        cleans.Sort((a, b) => a.DayIndex != b.DayIndex
            ? a.DayIndex.CompareTo(b.DayIndex)
            : string.CompareOrdinal(a.Time, b.Time));

        return new WeeklyScheduleResponse(robotId, cleans);
    }
}

/// <summary>One line of a schedule-write request: clean on these weekdays at this time.</summary>
public record ScheduleEntryRequest(
    int[] Days,
    string Time
);

/// <summary>
/// Replace the robot's weekly schedule with these entries. Days are weekday indices
/// (0 = Sunday .. 6 = Saturday); Time is "HH:MM" (24h). An empty list clears the schedule.
/// </summary>
public record SetScheduleRequest(
    List<ScheduleEntryRequest> Entries
);
