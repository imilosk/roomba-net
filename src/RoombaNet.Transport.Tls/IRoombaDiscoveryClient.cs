namespace RoombaNet.Transport.Tls;

public interface IRoombaDiscoveryClient
{
    /// <summary>
    /// Discovers all Roombas on the local network using UDP broadcast.
    /// </summary>
    /// <param name="timeoutSeconds">How long to wait for responses in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of discovered Roombas with their information.</returns>
    Task<List<RoombaInfo>> DiscoverRoombas(int timeoutSeconds = 5, CancellationToken cancellationToken = default);
}
