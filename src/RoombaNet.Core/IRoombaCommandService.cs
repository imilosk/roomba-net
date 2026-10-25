using RoombaNet.Core.Payloads;

namespace RoombaNet.Core;

public interface IRoombaCommandService
{
    Task Find(CancellationToken cancellationToken = default);
    Task Start(CancellationToken cancellationToken = default);
    Task Stop(CancellationToken cancellationToken = default);
    Task Pause(CancellationToken cancellationToken = default);
    Task Resume(CancellationToken cancellationToken = default);
    Task Dock(CancellationToken cancellationToken = default);
    Task Evac(CancellationToken cancellationToken = default);
    Task Reset(CancellationToken cancellationToken = default);
    Task Train(CancellationToken cancellationToken = default);
    Task Spot(CancellationToken cancellationToken = default);
    Task Wake(CancellationToken cancellationToken = default);
    Task Sleep(CancellationToken cancellationToken = default);
    Task Off(CancellationToken cancellationToken = default);
    Task Tidy(CancellationToken cancellationToken = default);
    Task Skip(CancellationToken cancellationToken = default);
    Task StopEvac(CancellationToken cancellationToken = default);
    Task WifiOn(CancellationToken cancellationToken = default);
    Task WifiOff(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an arbitrary command string to the <c>cmd</c> topic. Enables the long tail of
    /// device-specific commands (e.g. washpad, drypad, flushsluice) without a dedicated method.
    /// </summary>
    Task Send(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a targeted clean of specific regions/zones within a persistent map.
    /// </summary>
    Task CleanRegions(
        string pmapId,
        IReadOnlyList<Region> regions,
        string? userPmapvId = null,
        bool ordered = true,
        CancellationToken cancellationToken = default
    );
}
