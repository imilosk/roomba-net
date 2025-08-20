using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public class RoombaApiService : IRoombaApiService
{
    private readonly IRoombaCommandClient _commandClient;
    private readonly ILogger<RoombaApiService> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly string[] AvailableCommands =
    [
        "find", "start", "stop", "pause", "resume", "dock", "evac", "reset", "train",
    ];

    public RoombaApiService(
        IRoombaCommandClient commandClient,
        ILogger<RoombaApiService> logger,
        TimeProvider timeProvider)
    {
        _commandClient = commandClient;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResponse> ExecuteCommandAsync(
        string command,
        CancellationToken cancellationToken = default
    )
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Executing command '{Command}' with execution ID '{ExecutionId}'",
                command,
                executionId
            );

            if (!IsValidCommand(command))
            {
                var invalidMessage = $"Invalid command: {command}";
                _logger.LogWarning(invalidMessage);

                return new CommandResponse(
                    Success: false,
                    Command: command,
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidCommand",
                    Details: $"Available commands: {string.Join(", ", AvailableCommands)}"
                );
            }

            await ExecuteRoombaCommand(command, cancellationToken);

            var successMessage = $"Command '{command}' executed successfully";
            _logger.LogInformation(successMessage);

            return new CommandResponse(
                Success: true,
                Command: command,
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to execute command '{command}'";
            _logger.LogError(ex, errorMessage);

            return new CommandResponse(
                Success: false,
                Command: command,
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<BatchCommandResponse> ExecuteBatchCommandsAsync(
        BatchCommandRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;
        var results = new List<BatchCommandResult>();

        _logger.LogInformation("Executing batch commands with execution ID '{ExecutionId}'. Sequential: {Sequential}",
            executionId, request.Sequential);

        if (request.Sequential)
        {
            foreach (var command in request.Commands)
            {
                var result = await ExecuteSingleBatchCommand(command, cancellationToken);
                results.Add(result);

                // If a command fails and we're running sequentially, we might want to continue or stop
                // For now, we'll continue with all commands
            }
        }
        else
        {
            var tasks = request.Commands
                .Select(command => ExecuteSingleBatchCommand(command, cancellationToken));
            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);
        }

        var overallSuccess = results.All(r => r.Success);

        return new BatchCommandResponse(
            Success: overallSuccess,
            Results: results.ToArray(),
            Timestamp: timestamp,
            ExecutionId: executionId
        );
    }

    public Task<AvailableCommandsResponse> GetAvailableCommandsAsync()
    {
        var response = new AvailableCommandsResponse(
            Commands: AvailableCommands,
            Timestamp: _timeProvider.GetUtcNow().DateTime
        );

        return Task.FromResult(response);
    }

    public async Task<HealthCheckResponse> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // We could implement a simple ping command or connection check here
            // For now, we'll assume the service is healthy if we can create the response
            _logger.LogInformation("Health check requested");

            await Task.CompletedTask; // Simulate a health check operation)

            return new HealthCheckResponse(
                IsHealthy: true,
                Status: "Healthy - Roomba service is operational",
                Timestamp: _timeProvider.GetUtcNow().DateTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            return new HealthCheckResponse(
                IsHealthy: false,
                Status: "Unhealthy - Roomba service is not operational",
                Timestamp: _timeProvider.GetUtcNow().DateTime,
                Error: ex.Message
            );
        }
    }

    private async Task<BatchCommandResult> ExecuteSingleBatchCommand(
        string command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!IsValidCommand(command))
            {
                return new BatchCommandResult(
                    Command: command,
                    Success: false,
                    Message: $"Invalid command: {command}",
                    Error: "InvalidCommand"
                );
            }

            await ExecuteRoombaCommand(command, cancellationToken);

            return new BatchCommandResult(
                Command: command,
                Success: true,
                Message: $"Command '{command}' executed successfully"
            );
        }
        catch (Exception ex)
        {
            return new BatchCommandResult(
                Command: command,
                Success: false,
                Message: $"Failed to execute command '{command}'",
                Error: ex.Message
            );
        }
    }

    private async Task ExecuteRoombaCommand(string command, CancellationToken cancellationToken)
    {
        switch (command.ToLowerInvariant())
        {
            case "find":
                await _commandClient.Find(cancellationToken);
                break;
            case "start":
                await _commandClient.Start(cancellationToken);
                break;
            case "stop":
                await _commandClient.Stop(cancellationToken);
                break;
            case "pause":
                await _commandClient.Pause(cancellationToken);
                break;
            case "resume":
                await _commandClient.Resume(cancellationToken);
                break;
            case "dock":
                await _commandClient.Dock(cancellationToken);
                break;
            case "evac":
                await _commandClient.Evac(cancellationToken);
                break;
            case "reset":
                await _commandClient.Reset(cancellationToken);
                break;
            case "train":
                await _commandClient.Train(cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown command: {command}");
        }
    }

    private static bool IsValidCommand(string command)
    {
        return AvailableCommands.Contains(command.ToLowerInvariant());
    }
}