using RoombaNet.Core.Constants;

namespace RoombaNet.Core;

public interface IRoombaSettingsClient
{
    Task SetChildLock(bool enable, CancellationToken cancellationToken = default);
    Task SetBinPause(bool enable, CancellationToken cancellationToken = default);
    Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default);
    Task<string> GetPassword(CancellationToken cancellationToken = default);
    Task<string> GetIpAddress(CancellationToken cancellationToken = default);
    Task<string> GetBlid(CancellationToken cancellationToken = default);
}
