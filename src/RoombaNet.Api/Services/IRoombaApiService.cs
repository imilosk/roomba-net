using RoombaNet.Api.Models;

namespace RoombaNet.Api.Services;

public interface IRoombaApiService
{
    Task<CommandResponse> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

    Task<BatchCommandResponse> ExecuteBatchCommandsAsync(BatchCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<AvailableCommandsResponse> GetAvailableCommandsAsync();
    Task<HealthCheckResponse> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    Task<SettingsResponse> SetChildLockAsync(bool enable, CancellationToken cancellationToken = default);
    Task<SettingsResponse> SetBinPauseAsync(bool enable, CancellationToken cancellationToken = default);
    Task<SettingsResponse> SetCleaningPassesAsync(int passes, CancellationToken cancellationToken = default);
}