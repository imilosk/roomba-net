using System.Net;
using System.Net.Sockets;

namespace RoombaNet.Transport.Tls;

/// <summary>
/// Parses unicast discovery targets. UDP broadcast does not cross subnets/VLANs, so robots on a
/// separate IoT network can only be found by sending the discovery message to each address directly.
/// </summary>
public static class DiscoveryTargets
{
    // A /16 is 65k packets per round; anything wider is almost certainly a typo.
    private const int MinPrefixLength = 16;

    /// <summary>
    /// Expands targets such as "192.168.20.65" or "192.168.20.0/24" into IPv4 addresses.
    /// Network and broadcast addresses are skipped for CIDR ranges.
    /// </summary>
    /// <exception cref="FormatException">A target is not an IPv4 address or a valid CIDR range.</exception>
    public static List<IPAddress> Parse(IEnumerable<string> targets)
    {
        var addresses = new List<IPAddress>();
        foreach (var target in targets)
        {
            addresses.AddRange(ParseOne(target.Trim()));
        }

        return addresses.Distinct().ToList();
    }

    private static IEnumerable<IPAddress> ParseOne(string target)
    {
        var parts = target.Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out var address) || address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new FormatException($"'{target}' is not an IPv4 address or CIDR range.");
        }

        if (parts.Length == 1)
        {
            return [address];
        }

        if (!int.TryParse(parts[1], out var prefix) || prefix is < MinPrefixLength or > 32)
        {
            throw new FormatException($"'{target}' must use a prefix length between /{MinPrefixLength} and /32.");
        }

        return ExpandRange(address, prefix);
    }

    private static IEnumerable<IPAddress> ExpandRange(IPAddress address, int prefix)
    {
        var bytes = address.GetAddressBytes();
        var value = (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        var mask = uint.MaxValue << (32 - prefix);
        var network = value & mask;
        var broadcast = network | ~mask;

        // /31 and /32 have no network/broadcast addresses to skip.
        var first = prefix >= 31 ? network : network + 1;
        var last = prefix >= 31 ? broadcast : broadcast - 1;

        for (var ip = first; ip <= last && ip >= first; ip++)
        {
            yield return new IPAddress([(byte)(ip >> 24), (byte)(ip >> 16), (byte)(ip >> 8), (byte)ip]);
        }
    }
}
