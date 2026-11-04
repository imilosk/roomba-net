using RoombaNet.Core;

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
    string? NotReadyCause,
    int? MinutesIntoMission,
    int? MapCount,
    bool? BinPresent,
    bool? BinFull,
    int? WifiRssi,
    int? WifiSnr,
    bool? DockKnown,
    string? SoftwareVersion,
    // Braava (mopping) fields; null on vacuums.
    string? DetectedPad,
    bool? TankPresent,
    int? TankLevelPct,
    bool? LidOpen,
    // Weekly schedule, parsed from cleanSchedule2 (one row per scheduled day).
    List<ScheduledClean> Schedule
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
        var cleaning = phase == "run" && RoombaStatusText.IsMissionActive(cycle);
        // A mission still mid-flight (cycle active) that is homing/charging will auto-resume.
        var willResume = RoombaStatusText.IsMissionActive(cycle)
                         && phase is "hmMidMsn" or "charge";
        var docked = phase is "charge" or "stop" or "evac" && !cleaning;

        var notReadyCause = hasNotReady
            ? RoombaStatusText.DescribeNotReadyCause(
                state.DetectedPad, state.TankPresent, state.TankLvl, state.LidOpen, state.Bin?.Present)
            : null;
        var activity = RoombaStatusText.DescribeActivity(phase, cycle, error, notReady, battery, notReadyCause);

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
            ErrorMessage: hasError ? RoombaStatusText.DescribeError(error!.Value) : null,
            NotReadyCode: hasNotReady ? notReady : null,
            NotReadyCause: notReadyCause,
            MinutesIntoMission: m?.MssnM,
            MapCount: mapCount,
            BinPresent: state.Bin?.Present,
            BinFull: state.Bin?.Full,
            WifiRssi: state.Signal?.Rssi,
            WifiSnr: state.Signal?.Snr,
            DockKnown: state.Dock?.Known,
            SoftwareVersion: state.SoftwareVer,
            DetectedPad: state.DetectedPad,
            TankPresent: state.TankPresent,
            TankLevelPct: state.TankLvl,
            LidOpen: state.LidOpen,
            Schedule: WeeklyScheduleResponse.From(robotId, state).Cleans
        );
    }
}
