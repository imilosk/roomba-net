using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services;

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

        MapCommandEndpoints(commandsGroup);
        MapSettingsEndpoints(settingsGroup);
        MapStatusEndpoints(statusGroup);
    }

    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // Execute individual command
        group.MapPost("/commands/{command}", async (
                string command,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync(command, cancellationToken);

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

        // Execute batch commands
        group.MapPost("/commands/batch", async (
                [FromBody] BatchCommandRequest request,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteBatchCommandsAsync(request, cancellationToken);

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

        // Get available commands
        group.MapGet("/commands", async (RoombaApiService roombaService) =>
            {
                var result = await roombaService.GetAvailableCommandsAsync();
                return Results.Ok(result);
            })
            .WithName("GetAvailableCommands")
            .WithSummary("Get list of available Roomba commands")
            .WithDescription("Returns a list of all available commands that can be executed")
            .Produces<AvailableCommandsResponse>();

        // Health check
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

        // Dedicated command endpoints
        group.MapPost("/start", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("start", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("StartCleaning")
            .WithSummary("Start cleaning")
            .WithDescription("Start the Roomba cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/stop", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("stop", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("StopCleaning")
            .WithSummary("Stop cleaning")
            .WithDescription("Stop the Roomba and cancel the current cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/pause", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("pause", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("PauseCleaning")
            .WithSummary("Pause cleaning")
            .WithDescription("Pause the current cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/resume", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("resume", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("ResumeCleaning")
            .WithSummary("Resume cleaning")
            .WithDescription("Resume a paused cleaning cycle")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/dock", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("dock", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("DockRoomba")
            .WithSummary("Return to dock")
            .WithDescription("Send the Roomba back to its charging dock")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/find", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("find", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("FindRoomba")
            .WithSummary("Find Roomba")
            .WithDescription("Play a sound to help locate the Roomba")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/evac", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("evac", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("EvacuateBin")
            .WithSummary("Evacuate bin")
            .WithDescription("Empty the Roomba's bin into the Clean Base (if available)")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/reset", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("reset", cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("ResetRoomba")
            .WithSummary("Reset Roomba")
            .WithDescription("Reset the Roomba to factory settings")
            .Produces<CommandResponse>()
            .Produces<CommandResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/train", async (
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.ExecuteCommandAsync("train", cancellationToken);
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
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetChildLockAsync(request.Enable, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetChildLock")
            .WithSummary("Set child lock")
            .WithDescription("Enable or disable the child lock feature to prevent unwanted button presses")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/bin-pause", async (
                [FromBody] EnableSettingRequest request,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetBinPauseAsync(request.Enable, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("SetBinPause")
            .WithSummary("Set bin pause")
            .WithDescription("Enable or disable bin pause - pauses cleaning when the bin is full")
            .Produces<SettingsResponse>()
            .Produces<SettingsResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/cleaning-passes", async (
                [FromBody] CleaningPassesRequest request,
                RoombaApiService roombaService,
                CancellationToken cancellationToken) =>
            {
                var result = await roombaService.SetCleaningPassesAsync(request.Passes, cancellationToken);
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
                RoombaStatusService statusService,
                HttpContext context,
                ILogger<RoombaApiService> logger,
                CancellationToken cancellationToken) =>
            {
                context.Response.Headers.Append("Content-Type", "text/event-stream");
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Connection", "keep-alive");

                await context.Response.Body.FlushAsync(cancellationToken);

                try
                {
                    // Send the last known status immediately when client connects
                    var lastStatus = statusService.GetLastStatus();
                    if (lastStatus != null)
                    {
                        var json = JsonSerializer.Serialize(lastStatus);
                        var message = $"event: status\ndata: {json}\n\n";
                        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                        
                        await context.Response.Body.WriteAsync(bytes, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                        
                        logger.LogDebug("Sent last known status to new client");
                    }

                    // Then stream new updates as they arrive
                    await foreach (var update in statusService.StatusUpdates.ReadAllAsync(cancellationToken))
                    {
                        var json = JsonSerializer.Serialize(update);
                        var message = $"event: status\ndata: {json}\n\n";
                        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                        
                        await context.Response.Body.WriteAsync(bytes, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Client disconnected, this is normal
                    logger.LogInformation("Client disconnected from status stream");
                }
            })
            .WithName("StreamRoombaStatus")
            .WithSummary("Stream real-time Roomba status updates")
            .WithDescription(
                "Server-Sent Events (SSE) endpoint that streams real-time status updates from the Roomba MQTT connection")
            .ExcludeFromDescription(); // SSE endpoints don't work well in OpenAPI docs
    }
}