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

    /// <summary>Set vacuum suction power (Eco / Auto carpet-boost / Performance).</summary>
    Task SetCleaningPower(RoombaCleaningPower power, CancellationToken cancellationToken = default);

    /// <summary>Enable or disable edge cleaning. Maps to the inverse of <c>openOnly</c>.</summary>
    Task SetEdgeClean(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Rename the robot (as shown in apps and discovery).</summary>
    Task SetName(string name, CancellationToken cancellationToken = default);
}
