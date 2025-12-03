using RoombaNet.Core.Constants;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public interface IRoombaSettingsService
{
    Task SetChildLock(bool enable, CancellationToken cancellationToken = default);
    Task SetBinPause(bool enable, CancellationToken cancellationToken = default);
    Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default);
    Task<string> GetPassword(string ipAddress, int port = 8883, CancellationToken cancellationToken = default);
    Task<List<RoombaInfo>> DiscoverRoombas(CancellationToken cancellationToken = default);
}
