namespace RoombaNet.Proxy;

/// <summary>
/// Topic constants and helpers shared by the proxy brokers and upstream connections.
/// </summary>
public static class ProxyTopics
{
    /// <summary>MQTT wildcard matching every non-reserved topic.</summary>
    public const string All = "#";

    /// <summary>Prefix for proxy-owned meta topics (status, snapshots) on the native broker.</summary>
    public const string ProxyPrefix = "roombaproxy";

    /// <summary>Prefix for the aggregated, namespaced view on the plain listener.</summary>
    public const string AggregatedPrefix = "roomba";

    /// <summary>Sender-client-id marker used for messages the proxy injects on behalf of a robot.</summary>
    public const string UpstreamSenderPrefix = "upstream:";

    /// <summary>
    /// Robot-bound topics the proxy relays from clients to the robot. Mirrors the working Python prototype.
    /// <c>shadow/get</c> is matched per-robot via <see cref="IsRobotBound"/>.
    /// </summary>
    private static readonly string[] StaticAllowlist =
    [
        "cmd",
        "delta",
        "wifictl",
        "local/rrtp/request",
        "cloud/rrtp/request",
    ];

    /// <summary>Returns the shadow root topic for a robot, e.g. <c>$aws/things/&lt;blid&gt;/shadow</c>.</summary>
    public static string ShadowRoot(string blid) => $"$aws/things/{blid}/shadow";

    /// <summary>The retained snapshot topic a drop-in client reads for the full reported shadow.</summary>
    public static string ShadowUpdateTopic(string blid) => $"{ShadowRoot(blid)}/update";

    /// <summary>The robot-bound shadow-get topic for a robot.</summary>
    public static string ShadowGetTopic(string blid) => $"{ShadowRoot(blid)}/get";

    /// <summary>Retained proxy status topic (online/offline) for a robot on the native broker.</summary>
    public static string StatusTopic(string blid) => $"{ProxyPrefix}/{blid}/status";

    /// <summary>
    /// True when <paramref name="topic"/> is allowed to be relayed from a client to robot <paramref name="blid"/>.
    /// </summary>
    public static bool IsRobotBound(string topic, string blid)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return false;
        }

        foreach (var allowed in StaticAllowlist)
        {
            if (string.Equals(topic, allowed, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return string.Equals(topic, ShadowGetTopic(blid), StringComparison.Ordinal);
    }

    /// <summary>Builds the sender marker for upstream-injected messages, e.g. <c>upstream:&lt;blid&gt;</c>.</summary>
    public static string UpstreamSender(string blid) => $"{UpstreamSenderPrefix}{blid}";

    /// <summary>Extracts the robot BLID from an upstream sender marker, or null if not an upstream marker.</summary>
    public static string? TryGetUpstreamBlid(string? senderClientId)
    {
        if (senderClientId is not null && senderClientId.StartsWith(UpstreamSenderPrefix, StringComparison.Ordinal))
        {
            return senderClientId[UpstreamSenderPrefix.Length..];
        }

        return null;
    }
}
