using System.Net;
using Microsoft.AspNetCore.Mvc;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services.RoombaClients;
using RoombaNet.Api.Services.RoombaStatus;
using RoombaNet.Api.Services.RobotRegistry;
using RoombaNet.Api.Services;
using RoombaNet.Core;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Api.Endpoints;

public static class RoombaEndpoints
{
    public static void MapRoombaEndpoints(this IEndpointRouteBuilder app)
    {
        var commandsGroup = app.MapGroup("/api/roomba")
            .WithTags("Roomba Commands");

        var settingsGroup = app.MapGroup("/api/roomba/settings")
            .WithTags("Roomba Settings");

        var statusGroup = app.MapGroup("/api/roomba/status")
            .WithTags("Roomba Status");

        var discoveryGroup = app.MapGroup("/api/roomba/discovery")
            .WithTags("Roomba Discovery");

        var robotsGroup = app.MapGroup("/api/roomba/robots")
            .WithTags("Roomba Robots");

        var wifiGroup = app.MapGroup("/api/roomba/wifi")
            .WithTags("Roomba Robots");

        var mapsGroup = app.MapGroup("/api/roomba/maps")
            .WithTags("Roomba Maps");

        MapCommandEndpoints(commandsGroup);
        MapSettingsEndpoints(settingsGroup);
        MapStatusEndpoints(statusGroup);
        MapDiscoveryEndpoints(discoveryGroup);
        MapRobotRegistryEndpoints(robotsGroup);
        MapWifiEndpoints(wifiGroup);
        MapMapsEndpoints(mapsGroup);
    }

    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/commands/{command}", async (
                string command,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync(command, robotId, cancellationToken);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("ExecuteCommand")
            .WithSummary("Execute a single Roomba command")
            .WithDescription(
                "Execute a single command such as start, stop, pause, resume, dock, find, evac, reset, or train")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest)
            .Produces<CommandResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost("/commands/batch", async (
                [FromBody] BatchCommandRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteBatchCommandsAsync(request, robotId, cancellationToken);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("ExecuteBatchCommands")
            .WithSummary("Execute multiple Roomba commands")
            .WithDescription("Execute multiple commands either sequentially or in parallel")
            .Produces<BatchCommandResponse>()
            .Produces<BatchCommandResponse>(StatusCodes.Status400BadRequest)
            .Produces<BatchCommandResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/commands", async (RoombaApiService roombaService) =>
            {
                var result = await roombaService.GetAvailableCommandsAsync();
                return Results.Ok(result);
            })
            .WithName("GetAvailableCommands")
            .WithSummary("Get list of available Roomba commands")
            .WithDescription("Returns a list of all available commands that can be executed")
            .Produces<AvailableCommandsResponse>();

        group.MapGet("/health", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.GetHealthStatusAsync(cancellationToken);

                return result.IsHealthy
                    ? Results.Ok(result)
                    : Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        detail: result.Error);
            })
            .WithName("GetHealthStatus")
            .WithSummary("Check Roomba service health")
            .WithDescription("Returns the health status of the Roomba service")
            .Produces<HealthCheckResponse>()
            .Produces(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/start", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("start", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("StartCleaning")
            .WithSummary("Start cleaning")
            .WithDescription("Start the Roomba cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/stop", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("stop", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("StopCleaning")
            .WithSummary("Stop cleaning")
            .WithDescription("Stop the Roomba and cancel the current cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/pause", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("pause", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("PauseCleaning")
            .WithSummary("Pause cleaning")
            .WithDescription("Pause the current cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/resume", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("resume", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("ResumeCleaning")
            .WithSummary("Resume cleaning")
            .WithDescription("Resume a paused cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/dock", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("dock", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("DockRoomba")
            .WithSummary("Return to dock")
            .WithDescription("Send the Roomba back to its charging dock")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/find", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("find", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("FindRoomba")
            .WithSummary("Find Roomba")
            .WithDescription("Play a sound to help locate the Roomba")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/evac", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("evac", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("EvacuateBin")
            .WithSummary("Evacuate bin")
            .WithDescription("Empty the Roomba's bin into the Clean Base (if available)")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/reset", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("reset", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("ResetRoomba")
            .WithSummary("Reset Roomba")
            .WithDescription("Reset the Roomba to factory settings")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/train", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("train", robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("TrainRoomba")
            .WithSummary("Train Roomba")
            .WithDescription("Start a training run to help the Roomba learn the space")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/cancel", async (
                [FromQuery] string robotId,
                [FromQuery] bool? dock,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.CancelAsync(robotId, dock ?? true, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("CancelMission")
            .WithSummary("Cancel the current mission")
            .WithDescription(
                "Stops the current mission so it will NOT auto-resume after charging (sends stop, then docks unless dock=false).")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/clean-regions", async (
                [FromBody] CleanRegionsRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.CleanRegionsAsync(request, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("CleanRegions")
            .WithSummary("Clean specific regions/zones")
            .WithDescription(
                "Start a targeted clean of specific rooms or zones within a persistent map. Requires a pmap_id and at least one region.")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/commands/raw/{command}", async (
                string command,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SendRawCommandAsync(command, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SendRawCommand")
            .WithSummary("Send an arbitrary command")
            .WithDescription(
                "Publishes any command string to the device `cmd` topic. Use for device-specific commands not covered by a named endpoint (e.g. washpad, drypad, flushsluice).")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);
    }

    private static void MapSettingsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/available", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var subscription = await statusManager.GetSubscription(robotId, cancellationToken);
                var settings = GetAvailableSettings(subscription.LastStatus.State);

                return Results.Ok(new AvailableSettingsResponse(robotId, settings));
            })
            .WithName("GetAvailableSettings")
            .WithSummary("Get available settings for a robot")
            .WithDescription("Returns a filtered list of settings based on the robot's reported state.")
            .Produces<AvailableSettingsResponse>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPost("/child-lock", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetChildLockAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetChildLock")
            .WithSummary("Set child lock")
            .WithDescription("Enable or disable the child lock feature to prevent unwanted button presses")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/bin-pause", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetBinPauseAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetBinPause")
            .WithSummary("Set bin pause")
            .WithDescription("Enable or disable bin pause - pauses cleaning when the bin is full")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/cleaning-passes", async (
                [FromBody] CleaningPassesRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetCleaningPassesAsync(request.Passes, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetCleaningPasses")
            .WithSummary("Set cleaning passes")
            .WithDescription("Set the number of cleaning passes: 1 (OnePass), 2 (TwoPass), or 3 (RoomSizeClean/Auto)")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/rank-overlap", async (
                [FromBody] RankOverlapRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetRankOverlapAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetRankOverlap")
            .WithSummary("Set mopping overlap (Braava)")
            .WithDescription("Set Braava rankOverlap with a percentage value from 0 to 100.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/liquid-amount", async (
                [FromBody] LiquidAmountRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetLiquidAmountAsync(
                    request.Value,
                    robotId,
                    cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetLiquidAmount")
            .WithSummary("Set liquid amount (Braava)")
            .WithDescription("Set Braava padWetness (liquid amount) with a raw integer value: 1 (Eco), 2 (Standard), 3 (Ultra).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/charging-light-pattern", async (
                [FromBody] ChargingLightPatternRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetChargingLightPatternAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetChargingLightPattern")
            .WithSummary("Set charging light pattern (Braava)")
            .WithDescription("Set Braava chrgLrPtrn with a raw integer value: 0 (Docking & charging), 1 (Docking only), 2 (No status lights).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/cleaning-power", async (
                [FromBody] CleaningPowerRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetCleaningPowerAsync(request.Power, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetCleaningPower")
            .WithSummary("Set vacuum suction power")
            .WithDescription("Set suction power: 0 (Eco), 1 (Auto carpet-boost), or 2 (Performance).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/edge-clean", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetEdgeCleanAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetEdgeClean")
            .WithSummary("Set edge cleaning")
            .WithDescription("Enable or disable edge/corner cleaning.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/name", async (
                [FromBody] SetNameRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetNameAsync(request.Name, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetRobotName")
            .WithSummary("Rename the robot")
            .WithDescription("Change the robot's display name.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/eco-charge", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetEcoChargeAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetEcoCharge")
            .WithSummary("Set battery-longevity eco charging")
            .WithDescription("Enable or disable eco charging (ecoCharge), which trades charge speed for battery longevity.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/map-upload", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetMapUploadAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetMapUpload")
            .WithSummary("Allow uploading the learned map to the cloud")
            .WithDescription("Enable or disable cloud map upload (mapUploadAllowed).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/map-learning", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetMapLearningAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetMapLearning")
            .WithSummary("Allow persistent-map learning")
            .WithDescription("Enable or disable persistent-map learning (pmapLearningAllowed).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/continuous-map-learning", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetContinuousMapLearningAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetContinuousMapLearning")
            .WithSummary("Allow continuous persistent-map learning")
            .WithDescription("Enable or disable continuous persistent-map learning (pmapCL).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/perimeter-pass", async (
                [FromBody] EnableSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetPerimeterPassAsync(request.Enable, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetPerimeterPass")
            .WithSummary("Set the perimeter/edge pass")
            .WithDescription("Enable or disable the perimeter pass (inverse of noPP).")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/bin-type-detect", async (
                [FromBody] IntSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetBinTypeDetectAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetBinTypeDetect")
            .WithSummary("Set bin-type auto-detection")
            .WithDescription("Set bin-type auto-detection (binTypeDetect) with a raw integer value.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/volume", async (
                [FromBody] IntSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetVolumeAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetVolume")
            .WithSummary("Set speaker volume")
            .WithDescription("Set the robot's speaker volume (nested audio.volume) with a value from 0 to 100.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/timezone", async (
                [FromBody] StringSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetTimezoneAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetTimezone")
            .WithSummary("Set the IANA timezone")
            .WithDescription("Set the robot's IANA timezone, e.g. \"Europe/Ljubljana\".")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/language", async (
                [FromBody] StringSettingRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetLanguageAsync(request.Value, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetLanguage")
            .WithSummary("Set the robot voice language")
            .WithDescription("Select the robot voice language (nested langs2.sLang), e.g. \"en-US_1\". The language pack must already be on the robot.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/schedule", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var state = await statusManager.GetCurrentState(robotId, TimeSpan.FromSeconds(8), cancellationToken);
                return Results.Ok(WeeklyScheduleResponse.From(robotId, state));
            })
            .WithName("GetSchedule")
            .WithSummary("Get the weekly cleaning schedule")
            .WithDescription(
                "Returns the robot's weekly schedule parsed from cleanSchedule2 (one row per scheduled day). The schedule is stored on the robot and fires on-device, so it works with no cloud connection.")
            .Produces<WeeklyScheduleResponse>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPost("/schedule", async (
                [FromBody] SetScheduleRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetScheduleAsync(request, robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetSchedule")
            .WithSummary("Set the weekly cleaning schedule")
            .WithDescription(
                "Replaces the weekly schedule with the given entries. Each entry lists weekday indices (0=Sunday..6=Saturday) and a HH:MM time. An empty entries list clears the schedule. Stored on-robot; runs with no cloud connection.")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);
    }

    private static List<string> GetAvailableSettings(RoombaState state)
    {
        var settings = new List<string>();

        if (state.ChildLock.HasValue)
        {
            settings.Add("childLock");
        }

        if (state.BinPause.HasValue)
        {
            settings.Add("binPause");
        }

        if (state.TwoPass.HasValue || state.NoAutoPasses.HasValue)
        {
            settings.Add("cleaningPasses");
        }

        if (state.RankOverlap.HasValue)
        {
            settings.Add("rankOverlap");
        }

        if (state.PadWetness?.Disposable.HasValue == true || state.PadWetness?.Reusable.HasValue == true)
        {
            settings.Add("padWetness");
        }

        if (state.ChrgLrPtrn.HasValue)
        {
            settings.Add("chrgLrPtrn");
        }

        if (state.EcoCharge.HasValue)
        {
            settings.Add("ecoCharge");
        }

        if (state.MapUploadAllowed.HasValue)
        {
            settings.Add("mapUploadAllowed");
        }

        if (state.PmapLearningAllowed.HasValue)
        {
            settings.Add("pmapLearningAllowed");
        }

        if (state.BinTypeDetect.HasValue)
        {
            settings.Add("binTypeDetect");
        }

        if (state.NoPP.HasValue)
        {
            settings.Add("perimeterPass");
        }

        if (!string.IsNullOrWhiteSpace(state.Timezone))
        {
            settings.Add("timezone");
        }

        if (state.Langs2 is not null)
        {
            settings.Add("language");
        }

        return settings;
    }

    private static void MapStatusEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var state = await statusManager.GetCurrentState(robotId, TimeSpan.FromSeconds(8), cancellationToken);
                return Results.Ok(state);
            })
            .WithName("GetRoombaStatus")
            .WithSummary("Get the current Roomba status snapshot")
            .WithDescription(
                "Returns the latest known device state (battery, mission, bin, position, etc.). Waits briefly for the first status if the connection was just opened.")
            .Produces<RoombaState>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapGet("/summary", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var state = await statusManager.GetCurrentState(robotId, TimeSpan.FromSeconds(8), cancellationToken);
                return Results.Ok(RobotStatusSummary.From(robotId, state));
            })
            .WithName("GetRoombaStatusSummary")
            .WithSummary("Get a human-readable status summary")
            .WithDescription(
                "Returns a compact, interpreted status (activity, charging, will-resume, errors) derived from the raw shadow. The raw snapshot stays available at GET /status.")
            .Produces<RobotStatusSummary>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapGet("/firmware", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var state = await statusManager.GetCurrentState(robotId, TimeSpan.FromSeconds(8), cancellationToken);
                return Results.Ok(FirmwareInfo.From(robotId, state));
            })
            .WithName("GetRoombaFirmware")
            .WithSummary("Get the robot's firmware and module versions")
            .WithDescription(
                "Returns the robot's reported firmware: main software version, SKU, per-module versions (nav/mob/pwr/sft/linux/con), and dock firmware. Read-only — RoombaNet does not push OTA updates.")
            .Produces<FirmwareInfo>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapGet("/stream", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                HttpContext context,
                [FromServices] ILogger<RoombaApiService> logger,
                [FromServices] IHostApplicationLifetime lifetime,
                CancellationToken cancellationToken
            ) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("robotId is required", cancellationToken);
                    return;
                }

                context.Response.Headers.Append("Content-Type", "text/event-stream");
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                await context.Response.Body.FlushAsync(cancellationToken);

                // Combine the request cancellation token with the application stopping token
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    lifetime.ApplicationStopping
                );

                try
                {
                    var subscription = await statusManager.GetSubscription(robotId, cancellationToken);
                    var lastStatus = subscription.LastStatus;

                    var json = System.Text.Json.JsonSerializer.Serialize(lastStatus.State);
                    var message = $"event: status\ndata: {json}\n\n";
                    var bytes = System.Text.Encoding.UTF8.GetBytes(message);

                    await context.Response.Body.WriteAsync(bytes, cts.Token);
                    await context.Response.Body.FlushAsync(cts.Token);

                    logger.LogDebug("Sent last known status to new client");

                    await foreach (var update in subscription.Updates.ReadAllAsync(cts.Token))
                    {
                        json = System.Text.Json.JsonSerializer.Serialize(update.State);
                        message = $"event: status\ndata: {json}\n\n";
                        bytes = System.Text.Encoding.UTF8.GetBytes(message);

                        await context.Response.Body.WriteAsync(bytes, cts.Token);
                        await context.Response.Body.FlushAsync(cts.Token);

                        logger.LogDebug("Sent status update to client");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Client disconnected or application stopping, this is expected
                }
            })
            .WithName("StreamRoombaStatus")
            .WithSummary("Stream real-time Roomba status updates")
            .WithDescription(
                "Server-Sent Events (SSE) endpoint that streams real-time status updates from the Roomba MQTT connection")
            .ExcludeFromDescription(); // SSE endpoints don't work well in OpenAPI docs
    }

    private static void MapDiscoveryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                [FromQuery] int? timeout,
                [FromQuery] bool? save,
                [FromQuery] string[]? target,
                RoombaApiService roombaService,
                IRobotRegistry registry,
                CancellationToken cancellationToken) =>
            {
                List<IPAddress> targets;
                try
                {
                    targets = DiscoveryTargets.Parse(target ?? []);
                }
                catch (FormatException ex)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
                }

                var timeoutSeconds = timeout ?? 5;
                var result = await roombaService.DiscoverRoombas(timeoutSeconds, targets, cancellationToken);

                if (result.Success && save is true)
                {
                    foreach (var roomba in result.Roombas)
                    {
                        await registry.Create(new RobotCreateRequest
                        {
                            Blid = roomba.Blid,
                            Name = roomba.RobotName,
                            Ip = roomba.Ip,
                        }, cancellationToken);
                    }
                }

                return result.Success
                    ? Results.Ok(result)
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        detail: result.Error);
            })
            .WithName("DiscoverRoombas")
            .WithSummary("Discover Roomba robots on the network")
            .WithDescription(
                "Scans the local network for available Roomba robots using UDP broadcast. Broadcast does not cross subnets/VLANs, so pass target (repeatable; an IP or CIDR range such as 192.168.20.0/24) to also query those addresses directly. Returns a list of discovered robots with their network information. Set save=true to store them in the registry.")
            .Produces<DiscoveryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapRobotRegistryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                IRobotRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var robots = await registry.GetAll(cancellationToken);
                return Results.Ok(robots);
            })
            .WithName("ListRobots")
            .WithSummary("List registered Roombas")
            .WithDescription("Returns all Roombas stored in the local registry.")
            .Produces<List<RobotRecord>>();

        group.MapGet("/{blid}", async (
                string blid,
                IRobotRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var robot = await registry.Get(blid, cancellationToken);
                return robot is null ? Results.NotFound() : Results.Ok(robot);
            })
            .WithName("GetRobot")
            .WithSummary("Get a registered Roomba")
            .WithDescription("Returns a single Roomba record by BLID.")
            .Produces<RobotRecord>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                [FromBody] RobotCreateRequest request,
                IRobotRegistry registry,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var robot = await registry.Create(request, cancellationToken);
                    return Results.Created($"/api/roomba/robots/{robot.Blid}", robot);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        error = ex.Message,
                    });
                }
            })
            .WithName("CreateRobot")
            .WithSummary("Register a Roomba")
            .WithDescription("Adds or updates a Roomba entry in the local registry.")
            .Produces<RobotRecord>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{blid}", async (
                string blid,
                [FromBody] RobotUpdateRequest request,
                IRobotRegistry registry,
                IRoombaClientFactory clientFactory,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var robot = await registry.Update(blid, request, cancellationToken);
                    clientFactory.RemoveClient(blid);
                    return Results.Ok(robot);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound();
                }
            })
            .WithName("UpdateRobot")
            .WithSummary("Update a registered Roomba")
            .WithDescription("Updates a Roomba entry in the local registry.")
            .Produces<RobotRecord>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{blid}", async (
                string blid,
                IRobotRegistry registry,
                IRoombaClientFactory clientFactory,
                CancellationToken cancellationToken) =>
            {
                var removed = await registry.Delete(blid, cancellationToken);
                clientFactory.RemoveClient(blid);
                return removed ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteRobot")
            .WithSummary("Remove a Roomba")
            .WithDescription("Deletes a Roomba entry from the local registry.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{blid}/pair", async (
                string blid,
                IRobotRegistry registry,
                IRoombaClientFactory clientFactory,
                IRoombaPasswordService passwordService,
                CancellationToken cancellationToken) =>
            {
                var robot = await registry.Get(blid, cancellationToken);
                if (robot is null)
                {
                    return Results.NotFound();
                }

                var password = await passwordService.GetPassword(
                    Core.Constants.RoombaApDefaults.DefaultApAddress,
                    Core.Constants.RoombaApDefaults.DefaultApPort,
                    cancellationToken);

                if (string.IsNullOrEmpty(password))
                {
                    return Results.BadRequest(new RobotPairResponse
                    {
                        Success = false,
                        Message = "Password retrieval failed.",
                    });
                }

                await registry.UpdatePassword(blid, password, cancellationToken);
                clientFactory.RemoveClient(blid);
                var updated = await registry.Get(blid, cancellationToken);

                return Results.Ok(new RobotPairResponse
                {
                    Success = true,
                    Message = "Password retrieved and stored.",
                    Robot = updated
                });
            })
            .WithName("PairRobot")
            .WithSummary("Pair a Roomba and store its password")
            .WithDescription("Retrieves the password from a Roomba and stores it in the registry.")
            .Produces<RobotPairResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{blid}/password", async (
                string blid,
                IRobotRegistry registry,
                IRoombaPasswordService passwordService,
                CancellationToken cancellationToken) =>
            {
                var robot = await registry.Get(blid, cancellationToken);
                if (robot is null)
                {
                    return Results.NotFound();
                }

                var password = await passwordService.GetPassword(
                    Core.Constants.RoombaApDefaults.DefaultApAddress,
                    Core.Constants.RoombaApDefaults.DefaultApPort,
                    cancellationToken);

                if (string.IsNullOrEmpty(password))
                {
                    return Results.BadRequest(new RobotPasswordResponse
                    {
                        Success = false,
                        Message = "Password retrieval failed.",
                    });
                }

                return Results.Ok(new RobotPasswordResponse
                {
                    Success = true,
                    Message = "Password retrieved.",
                    Password = password
                });
            })
            .WithName("GetRobotPassword")
            .WithSummary("Retrieve a Roomba password without storing it")
            .WithDescription("Retrieves the password from a Roomba without updating the registry.")
            .Produces<RobotPasswordResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static void MapWifiEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/configure", async (
                [FromBody] WifiConfigureRequest request,
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ConfigureWifiAsync(
                    robotId,
                    request.Ssid,
                    request.Password,
                    cancellationToken);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("ConfigureWifi")
            .WithSummary("Configure Roomba Wi-Fi")
            .WithDescription("Configure Wi-Fi credentials while connected to the Roomba's access point.")
            .Produces<WifiConfigureResponse>()
            .Produces<WifiConfigureResponse>(StatusCodes.Status400BadRequest);
    }

    private static void MapMapsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                [FromQuery] string robotId,
                [FromServices] RoombaStatusManager statusManager,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(robotId))
                {
                    return Results.BadRequest("robotId is required");
                }

                var state = await statusManager.GetCurrentState(robotId, TimeSpan.FromSeconds(8), cancellationToken);
                var maps = (state.Pmaps ?? [])
                    .SelectMany(m => m.Select(kv => new MapInfo(kv.Key, kv.Value)))
                    .ToList();

                return Results.Ok(new MapListResponse(robotId, maps));
            })
            .WithName("ListMaps")
            .WithSummary("List persistent maps")
            .WithDescription("Returns the persistent maps (pmaps) currently stored on the robot.")
            .Produces<MapListResponse>()
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapDelete("/", async (
                [FromQuery] string robotId,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ClearMapsAsync(robotId, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("ClearMaps")
            .WithSummary("Delete all persistent maps")
            .WithDescription(
                "Clears every persistent map stored on the robot (local-only, no cloud). The robot builds a fresh map on its next mapping/clean run.")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);
    }
}
