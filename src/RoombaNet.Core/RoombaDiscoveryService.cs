using Microsoft.Extensions.Logging;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public class RoombaDiscoveryService : IRoombaDiscoveryService
{
    private readonly ILogger<RoombaDiscoveryService> _logger;
    private readonly IRoombaDiscoveryClient _discoveryClient;

    public RoombaDiscoveryService(
        ILogger<RoombaDiscoveryService> logger,
        IRoombaDiscoveryClient discoveryClient
    )
    {
        _logger = logger;
        _discoveryClient = discoveryClient;
    }

    public async Task<List<RoombaInfo>> DiscoverRoombas(
        int timeoutSeconds = 5,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Starting Roomba discovery with {Timeout}s timeout", timeoutSeconds);

        var roombas = await _discoveryClient.DiscoverRoombas(timeoutSeconds, cancellationToken);

        _logger.LogInformation("Discovery completed. Found {Count} Roomba device(s)", roombas.Count);

        return roombas;
    }
}
