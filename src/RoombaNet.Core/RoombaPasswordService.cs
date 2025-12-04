using Microsoft.Extensions.Logging;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public class RoombaPasswordService : IRoombaPasswordService
{
    private readonly ILogger<RoombaPasswordService> _logger;
    private readonly IRoombaPasswordClient _passwordClient;

    public RoombaPasswordService(
        ILogger<RoombaPasswordService> logger,
        IRoombaPasswordClient passwordClient
    )
    {
        _logger = logger;
        _passwordClient = passwordClient;
    }

    public async Task<string> GetPassword(
        string ipAddress,
        int port = 8883,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Retrieving password from Roomba at {IpAddress}:{Port}", ipAddress, port);

        var password = await _passwordClient.GetPassword(ipAddress, port, cancellationToken);

        if (string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Failed to retrieve password from Roomba at {IpAddress}:{Port}", ipAddress, port);
        }
        else
        {
            _logger.LogInformation(
                "Successfully retrieved password from Roomba at {IpAddress}:{Port}",
                ipAddress,
                port
            );
        }

        return password;
    }
}
