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
    private const int DelayBetweenBroadcastsMs = 500;

    public RoombaDiscoveryClient(ILogger<RoombaDiscoveryClient> logger)
    {
        _logger = logger;
    }

    public async Task<List<RoombaInfo>> DiscoverRoombas(
        int timeoutSeconds = 5,
        IReadOnlyCollection<IPAddress>? targets = null,
        CancellationToken cancellationToken = default
    )
    {
        using var udpClient = CreateUdpClient();
        var discoveredRoombas = new Dictionary<string, RoombaInfo>();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var listenTask = StartListeningForResponsesAsync(udpClient, discoveredRoombas, linkedCts.Token);

        await SendDiscoveryMessagesAsync(udpClient, targets ?? [], linkedCts.Token);

        await listenTask;

        _logger.LogInformation("Discovery completed. Found {Count} Roomba(s)", discoveredRoombas.Count);
        return discoveredRoombas.Values.ToList();
    }

    private Task StartListeningForResponsesAsync(
        UdpClient udpClient,
        Dictionary<string, RoombaInfo> discoveredRoombas,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cancellationToken);
                    ProcessResponse(result.Buffer, discoveredRoombas);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when timeout occurs
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Roomba discovery");
            }
        }, cancellationToken);
    }

    private void ProcessResponse(byte[] buffer, Dictionary<string, RoombaInfo> discoveredRoombas)
    {
        var response = Encoding.UTF8.GetString(buffer);

        if (response == DiscoveryMessage)
        {
            _logger.LogDebug("Ignoring own broadcast message");
            return;
        }

        var roombaInfo = TryParseRoombaInfo(response);
        if (roombaInfo != null && TryAddRoomba(discoveredRoombas, roombaInfo))
        {
            LogDiscoveredRoomba(roombaInfo);
        }
    }

    private RoombaInfo? TryParseRoombaInfo(string response)
    {
        try
        {
            return JsonSerializer.Deserialize(response, RoombaInfoJsonContext.Default.RoombaInfo);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Roomba response: {Response}", response);
            return null;
        }
    }

    private static bool TryAddRoomba(Dictionary<string, RoombaInfo> discoveredRoombas, RoombaInfo roombaInfo)
    {
        return !string.IsNullOrEmpty(roombaInfo.Ip) && discoveredRoombas.TryAdd(roombaInfo.Ip, roombaInfo);
    }

    private void LogDiscoveredRoomba(RoombaInfo roombaInfo)
    {
        _logger.LogInformation(
            "Discovered Roomba: {Name} ({Hostname}) at {Ip}, SKU: {Sku}",
            roombaInfo.RobotName,
            roombaInfo.Hostname,
            roombaInfo.Ip,
            roombaInfo.Sku
        );
    }

    private async Task SendDiscoveryMessagesAsync(
        UdpClient udpClient,
        IReadOnlyCollection<IPAddress> targets,
        CancellationToken cancellationToken)
    {
        var message = Encoding.UTF8.GetBytes(DiscoveryMessage);
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);

        try
        {
            for (var i = 0; i < BroadcastCount; i++)
            {
                await udpClient.SendAsync(message, message.Length, broadcastEndpoint);
                _logger.LogDebug("Broadcast message sent: {Count}", i + 1);

                foreach (var target in targets)
                {
                    await SendUnicastAsync(udpClient, message, new IPEndPoint(target, UdpPort));
                }

                if (targets.Count > 0)
                {
                    _logger.LogDebug("Unicast message sent to {Count} target(s)", targets.Count);
                }

                if (i < BroadcastCount - 1)
                {
                    await Task.Delay(DelayBetweenBroadcastsMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout occurs
        }
    }

    private async Task SendUnicastAsync(UdpClient udpClient, byte[] message, IPEndPoint endpoint)
    {
        try
        {
            await udpClient.SendAsync(message, message.Length, endpoint);
        }
        catch (SocketException ex)
        {
            // One unreachable address (e.g. no route) must not abort the rest of a range scan.
            _logger.LogDebug("Unicast discovery to {Endpoint} failed: {Error}", endpoint, ex.SocketErrorCode);
        }
    }

    private static UdpClient CreateUdpClient()
    {
        var client = new UdpClient(new IPEndPoint(IPAddress.Any, 0))
        {
            EnableBroadcast = true,
        };
        client.Client.ReceiveTimeout = 5000;
        return client;
    }
}