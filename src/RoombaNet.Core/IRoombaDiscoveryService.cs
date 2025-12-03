using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

/// <summary>
/// Provides functionality to discover Roomba devices on the network.
/// </summary>
public interface IRoombaDiscoveryService
{
    /// <summary>
    /// Discovers Roomba devices on the local network via UDP broadcast.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds to wait for responses. Default is 5 seconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of discovered Roomba devices with their connection information.</returns>
    Task<List<RoombaInfo>> DiscoverRoombas(int timeoutSeconds = 5, CancellationToken cancellationToken = default);
}
