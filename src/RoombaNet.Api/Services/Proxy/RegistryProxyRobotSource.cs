using RoombaNet.Api.Services.RobotRegistry;
using RoombaNet.Proxy.Configuration;

namespace RoombaNet.Api.Services.Proxy;

/// <summary>
/// Supplies the proxy's robot list from the API robot registry, so the proxy holds a connection to every
/// registered robot that has stored credentials.
/// </summary>
public sealed class RegistryProxyRobotSource : IProxyRobotSource
{
    private readonly IRobotRegistry _registry;
    private readonly ILogger<RegistryProxyRobotSource> _logger;

    public RegistryProxyRobotSource(IRobotRegistry registry, ILogger<RegistryProxyRobotSource> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProxyRobotDescriptor>> GetRobotsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _registry.GetAll(cancellationToken);
        var result = new List<ProxyRobotDescriptor>();

        foreach (var record in records)
        {
            var credentials = await _registry.GetCredentials(record.Blid, cancellationToken);
            if (credentials is null)
            {
                _logger.LogWarning("Skipping robot {Blid}: no stored credentials", record.Blid);
                continue;
            }

            result.Add(new ProxyRobotDescriptor
            {
                Blid = credentials.Blid,
                Ip = credentials.Ip,
                Port = credentials.Port,
                Password = credentials.Password,
                UseTls = true,
            });
        }

        return result;
    }
}
