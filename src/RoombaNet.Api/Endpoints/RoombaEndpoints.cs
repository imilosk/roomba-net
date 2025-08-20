using Microsoft.AspNetCore.Mvc;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services;

namespace RoombaNet.Api.Endpoints;

public static class RoombaEndpoints
{
    public static void MapRoombaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roomba")
            .WithTags("Roomba Commands")
            .WithOpenApi();

        // Execute individual command
        group.MapPost("/commands/{command}", async (
                string command,
                IRoombaApiService roombaService,
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
                IRoombaApiService roombaService,
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
        group.MapGet("/commands", async (IRoombaApiService roombaService) =>
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
                IRoombaApiService roombaService,
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
    }
}