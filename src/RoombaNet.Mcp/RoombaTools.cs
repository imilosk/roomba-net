using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RoombaNet.Mcp;

/// <summary>
/// Local, cloud-free Roomba control exposed as MCP tools. Every tool bridges to the
/// RoombaNet HTTP API on the local network.
/// </summary>
[McpServerToolType]
public class RoombaTools
{
    private readonly RoombaApiClient _api;

    public RoombaTools(RoombaApiClient api)
    {
        _api = api;
    }

    private static string Q(string robotId) => $"?robotId={RoombaApiClient.Escape(robotId)}";

    [McpServerTool(Name = "list_robots")]
    [Description("List all Roombas registered in the local registry (blid, name, ip).")]
    public Task<string> ListRobots(CancellationToken cancellationToken)
        => _api.GetJson("/api/roomba/robots", cancellationToken);

    [McpServerTool(Name = "discover_robots")]
    [Description("Scan the local network for Roomba robots and return the ones found.")]
    public Task<string> Discover(
        [Description("Optional IPs or CIDR ranges (e.g. 192.168.20.0/24) to query directly. Use when robots are on another subnet/VLAN, which broadcast cannot reach.")]
        string[]? targets,
        CancellationToken cancellationToken)
    {
        var query = targets is { Length: > 0 }
            ? "?" + string.Join("&", targets.Select(t => $"target={RoombaApiClient.Escape(t)}"))
            : "";
        return _api.GetJson($"/api/roomba/discovery{query}", cancellationToken);
    }

    [McpServerTool(Name = "get_status")]
    [Description("Get the full raw status snapshot for a robot: battery, mission phase/cycle, bin, position, map count, etc.")]
    public Task<string> GetStatus(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.GetJson($"/api/roomba/status{Q(robotId)}", cancellationToken);

    [McpServerTool(Name = "get_status_summary")]
    [Description("Get a compact, human-readable status: activity (cleaning/docked/returning-to-recharge/stuck/...), charging, will-resume, errors. Best for a quick check.")]
    public Task<string> GetStatusSummary(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.GetJson($"/api/roomba/status/summary{Q(robotId)}", cancellationToken);

    [McpServerTool(Name = "start_cleaning")]
    [Description("Start a cleaning mission.")]
    public Task<string> Start(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/start{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "stop_cleaning")]
    [Description("Stop the current cleaning mission.")]
    public Task<string> Stop(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/stop{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "pause_cleaning")]
    [Description("Pause the current cleaning mission.")]
    public Task<string> Pause(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/pause{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "resume_cleaning")]
    [Description("Resume a paused cleaning mission.")]
    public Task<string> Resume(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/resume{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "dock")]
    [Description("Send the robot back to its charging dock.")]
    public Task<string> Dock(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/dock{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "cancel_cleaning")]
    [Description("Cancel the current mission so it will NOT auto-resume after charging (stops, then docks). Use this instead of pause when the user wants to fully stop.")]
    public Task<string> Cancel(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/cancel{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "find")]
    [Description("Play a sound so you can locate the robot.")]
    public Task<string> Find(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/find{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "evacuate_bin")]
    [Description("Empty the robot's bin into the Clean Base (auto-evacuation models only).")]
    public Task<string> Evac(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/evac{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "send_command")]
    [Description("Send any supported command (find,start,stop,pause,resume,dock,evac,reset,train,spot,wake,sleep,off,tidy,skip,stopevac,wlapon,wlapoff).")]
    public Task<string> SendCommand(
        [Description("The robot's BLID / registry id")] string robotId,
        [Description("The command name")] string command,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/commands/{RoombaApiClient.Escape(command)}{Q(robotId)}", null, cancellationToken);

    [McpServerTool(Name = "list_maps")]
    [Description("List the persistent maps (pmaps) stored on the robot.")]
    public Task<string> ListMaps(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.GetJson($"/api/roomba/maps{Q(robotId)}", cancellationToken);

    [McpServerTool(Name = "clear_maps")]
    [Description("Delete ALL persistent maps on the robot (local-only, no cloud). The robot remaps on its next run. Irreversible.")]
    public Task<string> ClearMaps(
        [Description("The robot's BLID / registry id")] string robotId,
        CancellationToken cancellationToken)
        => _api.DeleteJson($"/api/roomba/maps{Q(robotId)}", cancellationToken);

    [McpServerTool(Name = "clean_regions")]
    [Description("Clean specific rooms/zones within a persistent map.")]
    public Task<string> CleanRegions(
        [Description("The robot's BLID / registry id")] string robotId,
        [Description("The persistent map id (pmap_id)")] string pmapId,
        [Description("Region ids to clean, e.g. [\"1\",\"3\"]")] string[] regionIds,
        [Description("Optional map version (user_pmapv_id)")] string? userPmapvId,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            pmapId,
            regions = regionIds.Select(id => new { regionId = id, type = "rid" }).ToArray(),
            userPmapvId,
            ordered = true,
        };
        return _api.PostJson($"/api/roomba/clean-regions{Q(robotId)}", body, cancellationToken);
    }

    [McpServerTool(Name = "set_cleaning_power")]
    [Description("Set vacuum suction power: 0 (Eco), 1 (Auto carpet-boost), 2 (Performance).")]
    public Task<string> SetCleaningPower(
        [Description("The robot's BLID / registry id")] string robotId,
        [Description("Power level: 0, 1, or 2")] int power,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/settings/cleaning-power{Q(robotId)}", new { power }, cancellationToken);

    [McpServerTool(Name = "set_cleaning_passes")]
    [Description("Set number of cleaning passes: 1 (One pass), 2 (Two passes), 3 (Auto/room-size).")]
    public Task<string> SetCleaningPasses(
        [Description("The robot's BLID / registry id")] string robotId,
        [Description("Passes: 1, 2, or 3")] int passes,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/settings/cleaning-passes{Q(robotId)}", new { passes }, cancellationToken);

    [McpServerTool(Name = "set_child_lock")]
    [Description("Enable or disable the child/pet lock.")]
    public Task<string> SetChildLock(
        [Description("The robot's BLID / registry id")] string robotId,
        [Description("true to enable, false to disable")] bool enable,
        CancellationToken cancellationToken)
        => _api.PostJson($"/api/roomba/settings/child-lock{Q(robotId)}", new { enable }, cancellationToken);
}
