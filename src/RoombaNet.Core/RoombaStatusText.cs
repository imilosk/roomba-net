namespace RoombaNet.Core;

/// <summary>
/// Turns raw shadow-state fields into human-readable status text.
/// Shared by the CLI and the API so both describe the robot the same way.
///
/// Code tables are mapped 1:1 from the iRobot Home Android app string resources
/// (error -> history_error_N, notReady -> history_start_refuse_N,
/// evac -> autoevac_history_complete_error_N). Verified against live robots:
/// notReady 34 = "Unrecognized cleaning pad", notReady 68 = "Map was unavailable".
/// </summary>
public static class RoombaStatusText
{
    public static bool IsMissionActive(string? cycle) => cycle is "clean" or "quick" or "spot";

    public static string DescribeActivity(
        string? phase,
        string? cycle,
        int? error,
        int? notReady,
        int? battery,
        string? notReadyCause = null)
    {
        if (error is not (null or 0))
        {
            return $"Error: {DescribeError(error.Value)} (code {error})";
        }

        if (notReady is not (null or 0))
        {
            // Prefer a live sensor-derived cause when available; otherwise use the
            // app's start-refuse table so every non-zero code gets a meaning.
            var cause = notReadyCause ?? DescribeNotReady(notReady.Value);
            return $"Not ready: {cause} (code {notReady})";
        }

        var missionActive = IsMissionActive(cycle);

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

    /// <summary>
    /// Describes a cleanMissionStatus.error code. Mapped from the app's
    /// history_error_N string family.
    /// </summary>
    public static string DescribeError(int code) =>
        code == 0 ? "none"
        : ErrorCodes.TryGetValue(code, out var text) ? text
        : $"unknown error (code {code})";

    /// <summary>
    /// Describes a cleanMissionStatus.notReady code. Mapped from the app's
    /// history_start_refuse_N string family.
    /// </summary>
    public static string DescribeNotReady(int code) =>
        code == 0 ? "ready"
        : NotReadyCodes.TryGetValue(code, out var text) ? text
        : $"not ready (code {code})";

    /// <summary>
    /// Describes a Clean Base auto-evacuation error code (350-360).
    /// Mapped from the app's autoevac_history_complete_error_N string family.
    /// </summary>
    public static string DescribeEvacError(int code) =>
        EvacErrorCodes.TryGetValue(code, out var text) ? text
        : $"unknown evac error (code {code})";

    private static readonly Dictionary<int, string> ErrorCodes = new()
    {
        { 1, "Left wheel off floor" },
        { 2, "Brushes need cleaning" },
        { 3, "Right wheel off floor" },
        { 4, "Left wheel stuck" },
        { 5, "Right wheel stuck" },
        { 6, "Stuck near a cliff" },
        { 7, "Left wheel error" },
        { 8, "Bin error" },
        { 9, "Bumper stuck" },
        { 10, "Right wheel error" },
        { 11, "Bin error" },
        { 12, "Cliff sensor issue" },
        { 13, "Both wheels off floor" },
        { 14, "Bin missing" },
        { 15, "Robot rebooted" },
        { 16, "Bumped unexpectedly" },
        { 17, "Path blocked" },
        { 18, "Docking issue" },
        { 19, "Undocking issue" },
        { 20, "Docking issue" },
        { 21, "Robot rebooted" },
        { 22, "Navigation problem" },
        { 23, "Battery issue" },
        { 24, "Navigation problem" },
        { 25, "Reboot required" },
        { 26, "Vacuum problem" },
        { 27, "Vacuum problem" },
        { 28, "Error" },
        { 29, "Software update needed" },
        { 30, "Vacuum problem" },
        { 31, "Reboot required" },
        { 32, "Smart map problem" },
        { 33, "Path blocked" },
        { 34, "Reboot required" },
        { 35, "Unable to mop" },
        { 36, "Bin and sensors need cleaning" },
        { 37, "Tank needed refilling" },
        { 38, "Robot error" },
        { 39, "Reboot required" },
        { 40, "Navigation problem" },
        { 41, "Timed out" },
        { 42, "Localization problem" },
        { 43, "Navigation problem" },
        { 44, "Pump issue" },
        { 45, "Lid open" },
        { 46, "Low battery" },
        { 47, "Reboot required" },
        { 48, "Path blocked" },
        { 52, "Pad required attention" },
        { 54, "Blades stuck" },
        { 55, "Left blades stuck" },
        { 56, "Right blades stuck" },
        { 57, "Cutting deck stuck" },
        { 58, "Navigation problem" },
        { 59, "Tilt detected" },
        { 60, "Rolled over" },
        { 62, "Stop button pushed" },
        { 63, "Hardware error" },
        { 65, "Hardware problem detected" },
        { 66, "Low memory" },
        { 67, "Handle lifted" },
        { 68, "Camera issue" },
        { 69, "Navigation problem" },
        { 70, "Problem sensing beacons" },
        { 73, "Pad type changed" },
        { 74, "Max Sq. Ft. reached" },
        { 75, "Navigation problem" },
        { 76, "Hardware problem detected" },
        { 78, "Left wheel error" },
        { 79, "Right wheel error" },
        { 85, "Path to charging station blocked" },
        { 86, "Path to charging station blocked" },
        { 88, "Navigation problem" },
        { 89, "Timed out" },
        { 91, "Workspace path error: Retrain the robot" },
        { 92, "Workspace path error: Retrain the robot" },
        { 93, "Workspace path error: Retrain the robot" },
        { 94, "Wheel motor over temp" },
        { 95, "Wheel motor under temp" },
        { 96, "Blade motor over temp" },
        { 97, "Blade motor under temp" },
        { 98, "Software error" },
        { 99, "Navigation problem" },
        { 121, "I can’t charge. Can you help?" },
        { 1000, "Left edge-sweeping brush stuck" },
        { 1001, "Right edge-sweeping brush stuck" },
        { 1002, "Cleaning unavailable. Check subscription status" },
        { 1003, "Dead vision board" },
        { 1004, "Map was unavailable" },
        { 1007, "Mop arm issue" },
        { 1008, "Mop arm stuck" },
        { 1009, "Robot stalled" },
        { 1010, "Clear the robot's path" },
        { 1011, "Carpet detection issue" },
        { 1012, "Couldn't reach viewpoint" },
        { 1013, "Check tank for leaks" },
        { 1014, "Sensor needs attention" },
        { 1015, "Sensor needs attention" },
        { 1019, "Incompatible cleaning head" },
    };

    private static readonly Dictionary<int, string> NotReadyCodes = new()
    {
        { 1, "the robot cannot start a cleaning job while detecting a cliff" },
        { 2, "Both wheels off floor" },
        { 3, "Left wheel off floor" },
        { 4, "Right wheel off floor" },
        { 5, "the robot was already trying to dock" },
        { 6, "the robot's main brushes were stuck" },
        { 7, "the robot's bin was missing" },
        { 8, "the robot needs to be rebooted" },
        { 9, "the robot's software was being updated" },
        { 10, "the robot cannot start while near a Virtual Wall" },
        { 11, "Vacuum problem" },
        { 12, "unknown (code 12)" },
        { 13, "unknown (code 13)" },
        { 14, "the robot was not ready for that request" },
        { 15, "Low battery" },
        { 16, "Bin full" },
        { 17, "Navigation problem" },
        { 18, "Software upgrade" },
        { 19, "Charging error" },
        { 21, "Reboot required" },
        { 22, "Navigation problem" },
        { 23, "Battery issue" },
        { 24, "Smart Map problem" },
        { 25, "Reboot required" },
        { 26, "Reboot required" },
        { 27, "Reboot required" },
        { 28, "Reboot required" },
        { 31, "Tank low" },
        { 32, "Lid open" },
        { 33, "Bumper stuck" },
        { 34, "Unrecognized cleaning pad" },
        { 35, "Unrecognized cleaning pad" },
        { 36, "Unable to empty bin: clog" },
        { 37, "Battery issue" },
        { 38, "Battery issue" },
        { 39, "Clean Map problem" },
        { 40, "Software update required" },
        { 51, "Hardware problem detected" },
        { 52, "Cancelled due to rain in the forecast" },
        { 53, "Left wheel error" },
        { 54, "Right wheel error" },
        { 55, "Not on charging station" },
        { 57, "Navigation problem" },
        { 58, "Workspace path error: Retrain the robot" },
        { 59, "Workspace path error: Retrain the robot" },
        { 60, "Workspace path error: Retrain the robot" },
        { 61, "Wheel motor over temp" },
        { 62, "Wheel motor under temp" },
        { 63, "Blade motor over temp" },
        { 64, "Blade motor under temp" },
        { 65, "Software error" },
        { 66, "Cleaning unavailable. Check subscription status" },
        { 67, "Hardware problem detected" },
        { 68, "Map was unavailable" },
        { 70, "Schedule blocked by Do Not Disturb" },
        { 72, "Check tank for leaks" },
        { 73, "Sensor needs attention" },
        { 74, "Sensor needs attention" },
        { 78, "Incompatible cleaning head" },
    };

    private static readonly Dictionary<int, string> EvacErrorCodes = new()
    {
        { 350, "Unable to empty bin: bag missing" },
        { 351, "Unable to empty bin: clog" },
        { 352, "Unable to empty bin: sealing problem" },
        { 353, "Unable to empty bin: bag full" },
        { 354, "Unable to empty bin: motor problem" },
        { 355, "Unable to empty bin: bag getting full" },
        { 360, "Unable to empty bin: communication problem" },
    };

    /// <summary>
    /// Explains a not-ready state from the robot's own sensor fields rather than from the
    /// not-ready code, whose meaning differs between models and isn't documented.
    /// Returns null when no reported condition explains it.
    /// </summary>
    public static string? DescribeNotReadyCause(
        string? detectedPad = null,
        bool? tankPresent = null,
        int? tankLevel = null,
        bool? lidOpen = null,
        bool? binPresent = null)
    {
        var causes = new List<string>();

        // Braava reports "invalid" when no (or an unrecognised) pad is attached.
        if (detectedPad == "invalid") causes.Add("no cleaning pad attached");
        if (tankPresent == false) causes.Add("water tank missing");
        else if (tankLevel == 0) causes.Add("water tank empty");
        if (lidOpen == true) causes.Add("tank lid open");
        if (binPresent == false) causes.Add("bin missing");

        return causes.Count == 0 ? null : string.Join(", ", causes);
    }

    public static string DescribePad(string? detectedPad) => detectedPad switch
    {
        null => "unknown",
        "invalid" => "none / not recognised",
        _ => detectedPad,
    };

    public static string DescribeLiquidAmount(int? padWetness) => padWetness switch
    {
        1 => "Eco",
        2 => "Standard",
        3 => "Ultra",
        null => "unknown",
        _ => padWetness.Value.ToString(),
    };
}
