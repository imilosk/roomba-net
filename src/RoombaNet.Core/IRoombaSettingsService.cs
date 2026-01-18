using RoombaNet.Core.Constants;

namespace RoombaNet.Core;

public interface IRoombaSettingsService
{
    Task SetChildLock(bool enable, CancellationToken cancellationToken = default);
    Task SetBinPause(bool enable, CancellationToken cancellationToken = default);
    Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default);
    Task SetRankOverlap(int value, CancellationToken cancellationToken = default);
    Task SetChargingLightPattern(int value, CancellationToken cancellationToken = default);
    Task SetPadWetness(int disposable, CancellationToken cancellationToken = default);
}
