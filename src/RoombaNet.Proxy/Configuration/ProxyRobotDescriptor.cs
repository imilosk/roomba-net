namespace RoombaNet.Proxy.Configuration;

/// <summary>
/// Identifies a single robot the proxy should hold the upstream connection for.
/// This is the source-agnostic shape consumed by the proxy; it can be built from
/// configuration (<c>Proxy:Robots</c> or <c>Robots:Profiles</c>) or from the API robot registry.
/// </summary>
public sealed class ProxyRobotDescriptor
{
    /// <summary>Robot BLID. Used as the upstream client-id and as the proxy auth username.</summary>
    public string Blid { get; init; } = string.Empty;

    /// <summary>Robot IP/host of the upstream MQTT endpoint.</summary>
    public string Ip { get; init; } = string.Empty;

    /// <summary>Upstream MQTT port (8883 for the robot's TLS endpoint).</summary>
    public int Port { get; init; } = 8883;

    /// <summary>Local robot password used both upstream and to authenticate drop-in clients.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Whether the upstream connection uses TLS. True for real robots. False is only used when the
    /// upstream endpoint is itself a plain broker (e.g. tests or a chained plain broker).
    /// </summary>
    public bool UseTls { get; init; } = true;
}
