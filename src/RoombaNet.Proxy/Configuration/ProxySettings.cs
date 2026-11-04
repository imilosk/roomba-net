namespace RoombaNet.Proxy.Configuration;

/// <summary>
/// Configuration for the multi-robot MQTT proxy. Bound from the <c>Proxy</c> configuration section.
/// </summary>
public sealed class ProxySettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Proxy";

    /// <summary>
    /// Master switch for the proxy hosted service. Defaults to false so that enabling the proxy
    /// (and the Phase 2 loopback refactor) is always an explicit opt-in and never changes existing behaviour.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Native, drop-in TLS listener settings (clients connect exactly as to a real robot).</summary>
    public TlsListenerSettings Tls { get; set; } = new();

    /// <summary>Convenience plain (no-TLS) localhost listener settings.</summary>
    public PlainListenerSettings Plain { get; set; } = new();

    /// <summary>
    /// Explicit list of robots to proxy. When empty, the robots are derived from <c>Robots:Profiles</c>.
    /// </summary>
    public List<ProxyRobotConfig> Robots { get; set; } = [];
}

/// <summary>Native drop-in TLS listener settings.</summary>
public sealed class TlsListenerSettings
{
    /// <summary>TLS listen port. 8883 matches the robot, so a client only changes the host IP.</summary>
    public int Port { get; set; } = 8883;

    /// <summary>Optional PKCS#12 (.pfx) certificate path. When unset, a self-signed cert is generated at startup.</summary>
    public string? CertificatePath { get; set; }

    /// <summary>Optional password for <see cref="CertificatePath"/>.</summary>
    public string? CertificatePassword { get; set; }
}

/// <summary>Convenience plain (no-TLS) localhost listener settings.</summary>
public sealed class PlainListenerSettings
{
    /// <summary>Whether the plain localhost listener is started.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Bind host. Kept on loopback by default because this listener performs no authentication.</summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>Plain listen port.</summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// When true, every robot is mirrored under <c>roomba/&lt;blid&gt;/&lt;topic&gt;</c> with a merged
    /// retained snapshot on <c>roomba/&lt;blid&gt;/state</c>, so a single subscription can watch all robots.
    /// </summary>
    public bool Aggregated { get; set; } = true;
}

/// <summary>Configuration shape for an entry in <c>Proxy:Robots</c>.</summary>
public sealed class ProxyRobotConfig
{
    /// <summary>Robot BLID.</summary>
    public string Blid { get; set; } = string.Empty;

    /// <summary>Robot IP/host.</summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>Upstream MQTT port.</summary>
    public int Port { get; set; } = 8883;

    /// <summary>Local robot password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Whether the upstream connection uses TLS.</summary>
    public bool UseTls { get; set; } = true;
}
