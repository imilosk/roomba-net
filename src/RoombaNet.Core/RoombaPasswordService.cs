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
        password = NormalizePassword(password);

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

    public async Task<bool> SetPassword(
        string ipAddress,
        string password,
        string assetId,
        string assetType,
        int port = 8883,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Password is required to set a new Roomba password.");
            return false;
        }

        _logger.LogInformation("Setting Roomba password at {IpAddress}:{Port}", ipAddress, port);

        try
        {
            await _passwordClient.SetPassword(
                ipAddress,
                password,
                assetId,
                assetType,
                port,
                cancellationToken);
            _logger.LogInformation("Password set request sent to Roomba at {IpAddress}:{Port}", ipAddress, port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Roomba password at {IpAddress}:{Port}", ipAddress, port);
            return false;
        }
    }

    private static string NormalizePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.StartsWith(":1:", StringComparison.Ordinal))
        {
            return password;
        }

        if (password.StartsWith(":", StringComparison.Ordinal))
        {
            return $":1{password}";
        }

        return $":1:{password}";
    }
}
