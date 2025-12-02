using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RoombaNet.Transport.Tls;

public class RoombaDiscoveryClient : IRoombaDiscoveryClient
{
    private readonly ILogger<RoombaDiscoveryClient> _logger;
    private const int UdpPort = 5678;
    private const string DiscoveryMessage = "irobotmcs";
    private const int BroadcastCount = 5;

    public RoombaDiscoveryClient(ILogger<RoombaDiscoveryClient> logger)
    {
        _logger = logger;
    }

    public async Task<List<RoombaInfo>> DiscoverRoombasAsync(
        int timeoutSeconds = 5,
        CancellationToken cancellationToken = default
    )
    {
        using var udpClient = CreateUdpClient();
        var message = Encoding.UTF8.GetBytes(DiscoveryMessage);
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);
        var discoveredRoombas = new Dictionary<string, RoombaInfo>(); // Use IP as key to avoid duplicates

        // Send broadcast messages multiple times to increase chances of discovery
        for (var i = 0; i < BroadcastCount; i++)
        {
            await udpClient.SendAsync(message, message.Length, broadcastEndpoint);
            _logger.LogDebug("Broadcast message sent: {Count}", i + 1);
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (true)
            {
                var result = await udpClient.ReceiveAsync(linkedCts.Token);
                var response = Encoding.UTF8.GetString(result.Buffer);

                // Ignore our own broadcast
                if (response == DiscoveryMessage)
                {
                    _logger.LogDebug("Ignoring own broadcast message");
                    continue;
                }

                try
                {
                    var roombaInfo = JsonSerializer.Deserialize(response, RoombaInfoJsonContext.Default.RoombaInfo);
                    if (roombaInfo != null && !string.IsNullOrEmpty(roombaInfo.Ip))
                    {
                        if (discoveredRoombas.TryAdd(roombaInfo.Ip, roombaInfo))
                        {
                            _logger.LogInformation(
                                "Discovered Roomba: {Name} ({Hostname}) at {Ip}, SKU: {Sku}",
                                roombaInfo.RobotName,
                                roombaInfo.Hostname,
                                roombaInfo.Ip,
                                roombaInfo.Sku
                            );
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Roomba response: {Response}", response);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Discovery completed. Found {Count} Roomba(s)", discoveredRoombas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Roomba discovery");
        }

        return discoveredRoombas.Values.ToList();
    }

    private static UdpClient CreateUdpClient()
    {
        var udpClient = new UdpClient
        {
            EnableBroadcast = true,
            Client =
            {
                ReceiveTimeout = 5000,
            },
        };
        return udpClient;
    }
}
