namespace RoombaNet.Core;

public interface IRoombaMapService
{
    /// <summary>
    /// Clears all persistent maps (pmaps) stored on the robot by publishing an empty
    /// <c>pmaps</c> array to the device shadow. The robot will build a fresh map on its
    /// next mapping/clean run. Entirely local — no cloud involved.
    /// </summary>
    Task<bool> ClearAllMaps(CancellationToken cancellationToken = default);
}
