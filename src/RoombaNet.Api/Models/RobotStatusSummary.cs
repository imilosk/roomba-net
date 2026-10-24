namespace RoombaNet.Api.Models;

/// <summary>
/// A compact, human-readable interpretation of the robot's raw shadow state.
/// Returned alongside (not instead of) the full state so callers can keep both.
/// </summary>
public record RobotStatusSummary(
    string RobotId,
    string Activity,
    string? Phase,
    string? Cycle,
    int? BatteryPct,
    bool Charging,
    bool Cleaning,
    bool WillResume,
    bool Docked,
    int? ErrorCode,
    string? ErrorMessage,
    int? NotReadyCode,
    int? MinutesIntoMission,
    int? MapCount,
    bool? BinPresent,
    bool? BinFull,
    int? WifiRssi,
    int? WifiSnr,
    bool? DockKnown
)
{
    public static RobotStatusSummary From(string robotId, RoombaState state)
    {
        var m = state.CleanMissionStatus;
        var phase = m?.Phase;
        var cycle = m?.Cycle;
        var error = m?.Error;
        var notReady = m?.NotReady;
        var battery = state.BatPct;
        var mapCount = state.Pmaps?.Count;

        var hasError = error is not (null or 0);
        var hasNotReady = notReady is not (null or 0);
        var charging = phase == "charge";
        var cleaning = phase == "run" && cycle is "clean" or "quick" or "spot";
        // A mission still mid-flight (cycle active) that is homing/charging will auto-resume.
        var willResume = cycle is "clean" or "quick" or "spot"
                         && phase is "hmMidMsn" or "charge";
        var docked = phase is "charge" or "stop" or "evac" && !cleaning;

        var activity = DescribeActivity(phase, cycle, error, notReady, battery);

        return new RobotStatusSummary(
            RobotId: robotId,
            Activity: activity,
            Phase: phase,
            Cycle: cycle,
            BatteryPct: battery,
            Charging: charging,
            Cleaning: cleaning,
            WillResume: willResume,
            Docked: docked,
            ErrorCode: hasError ? error : null,
            ErrorMessage: hasError ? DescribeError(error!.Value) : null,
            NotReadyCode: hasNotReady ? notReady : null,
            MinutesIntoMission: m?.MssnM,
            MapCount: mapCount,
            BinPresent: state.Bin?.Present,
            BinFull: state.Bin?.Full,
            WifiRssi: state.Signal?.Rssi,
            WifiSnr: state.Signal?.Snr,
            DockKnown: state.Dock?.Known
        );
    }

    private static string DescribeActivity(string? phase, string? cycle, int? error, int? notReady, int? battery)
    {
        if (error is not (null or 0))
        {
            return $"Error: {DescribeError(error.Value)} (code {error})";
        }

        if (notReady is not (null or 0))
        {
            return $"Not ready (code {notReady})";
        }

        var missionActive = cycle is "clean" or "quick" or "spot";

        return phase switch
        {
            "run" when cycle == "evac" => "Emptying bin",
            "run" => "Cleaning",
            "evac" => "Emptying bin",
            "stuck" => "Stuck — needs help",
            "hmMidMsn" => "Returning to dock to recharge (will resume mission)",
            "hmUsrDock" => "Returning to dock (requested)",
            "hmPostMsn" => "Returning to dock (mission complete)",
            "charge" when missionActive => "Docked and charging — will resume mission",
            "charge" when battery is >= 100 => "Docked — fully charged, idle",
            "charge" => "Docked and charging",
            "stop" when missionActive => "Paused",
            "stop" => "Idle (stopped)",
            "pause" => "Paused",
            "evacuating" => "Emptying bin",
            null => "Unknown",
            _ => phase,
        };
    }

    private static string DescribeError(int code) => code switch
    {
        0 => "none",
        1 => "wheel stuck / lifted",
        2 => "brushes stuck",
        6 => "cliff sensor issue",
        9 => "bumper stuck",
        15 => "internal error — reboot",
        16 => "bin full",
        18 => "docking failed",
        _ => "see iRobot error reference",
    };
}
