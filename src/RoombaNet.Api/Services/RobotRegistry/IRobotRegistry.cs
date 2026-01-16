using RoombaNet.Api.Models;

namespace RoombaNet.Api.Services.RobotRegistry;

public interface IRobotRegistry
{
    Task<IReadOnlyList<RobotRecord>> GetAll(CancellationToken cancellationToken = default);
    Task<RobotRecord?> Get(string blid, CancellationToken cancellationToken = default);
    Task<RobotCredentials?> GetCredentials(string blid, CancellationToken cancellationToken = default);
    Task<RobotRecord> Create(RobotCreateRequest request, CancellationToken cancellationToken = default);
    Task<RobotRecord> Update(string blid, RobotUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> Delete(string blid, CancellationToken cancellationToken = default);
    Task<bool> UpdatePassword(string blid, string password, CancellationToken cancellationToken = default);
}
