namespace RoombaNet.Proxy.Configuration;

/// <summary>
/// Supplies the set of robots the proxy should hold connections for. Implementations can read from
/// configuration (<c>Proxy:Robots</c> / <c>Robots:Profiles</c>) or from the API robot registry.
/// </summary>
public interface IProxyRobotSource
{
    /// <summary>Returns the robots to proxy.</summary>
    Task<IReadOnlyList<ProxyRobotDescriptor>> GetRobotsAsync(CancellationToken cancellationToken = default);
}
