using Microsoft.AspNetCore.Mvc;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services.RoombaClients;
using RoombaNet.Api.Services.RoombaStatus;
using RoombaNet.Api.Services.RobotRegistry;
using RoombaNet.Api.Services;
using RoombaNet.Core;

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

        MapCommandEndpoints(commandsGroup);
        MapSettingsEndpoints(settingsGroup);
        MapStatusEndpoints(statusGroup);
        MapDiscoveryEndpoints(discoveryGroup);
        MapRobotRegistryEndpoints(robotsGroup);
        MapWifiEndpoints(wifiGroup);
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
    }

    private static void MapSettingsEndpoints(RouteGroupBuilder group)
    {
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
    }

    private static void MapStatusEndpoints(RouteGroupBuilder group)
    {
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
                RoombaApiService roombaService,
                IRobotRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var timeoutSeconds = timeout ?? 5;
                var result = await roombaService.DiscoverRoombas(timeoutSeconds, cancellationToken);

                if (result.Success && save is true)
                {
                    foreach (var roomba in result.Roombas)
                    {
                        await registry.Create(new RobotCreateRequest
                        {
                            Blid = roomba.Blid,
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
                "Scans the local network for available Roomba robots using UDP broadcast. Returns a list of discovered robots with their network information. Set save=true to store them in the registry.")
            .Produces<DiscoveryResponse>()
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
}