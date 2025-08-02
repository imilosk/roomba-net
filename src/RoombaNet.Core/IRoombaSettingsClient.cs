using RoombaNet.Core.Constants;

namespace RoombaNet.Core;

public interface IRoombaSettingsClient
{
    Task ChildLock(bool enable, CancellationToken cancellationToken = default);
    Task BinPause(bool enable, CancellationToken cancellationToken = default);
    Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default);
}
