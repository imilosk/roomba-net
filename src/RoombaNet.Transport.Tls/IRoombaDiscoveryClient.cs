using System.Net;

namespace RoombaNet.Transport.Tls;

public interface IRoombaDiscoveryClient
{
    /// <summary>
    /// Discovers Roombas using UDP broadcast on the local subnet, plus unicast to any extra targets
    /// (needed for robots on another subnet/VLAN, which broadcast cannot reach).
    /// </summary>
    /// <param name="timeoutSeconds">How long to wait for responses in seconds.</param>
    /// <param name="targets">Extra addresses to query directly. See <see cref="DiscoveryTargets"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of discovered Roombas with their information.</returns>
    Task<List<RoombaInfo>> DiscoverRoombas(
        int timeoutSeconds = 5,
        IReadOnlyCollection<IPAddress>? targets = null,
        CancellationToken cancellationToken = default);
}
