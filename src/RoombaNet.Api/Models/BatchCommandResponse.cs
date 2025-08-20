namespace RoombaNet.Api.Models;

public record BatchCommandResponse(
    bool Success,
    BatchCommandResult[] Results,
    DateTime Timestamp,
    string ExecutionId
);

public record BatchCommandResult(
    string Command,
    bool Success,
    string Message,
    string? Error = null
);