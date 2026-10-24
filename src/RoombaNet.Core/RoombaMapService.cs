using Microsoft.Extensions.Logging;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Transport.Mqtt;
using RoombaJsonContext = RoombaNet.Core.Payloads.RoombaJsonContext;

namespace RoombaNet.Core;

public class RoombaMapService : IRoombaMapService
{
    private readonly ILogger<RoombaMapService> _logger;
    private readonly IRoombaConnectionManager _connectionManager;
    private readonly IMqttPublisher _mqttPublisher;

    public RoombaMapService(
        ILogger<RoombaMapService> logger,
        IRoombaConnectionManager connectionManager,
        IMqttPublisher mqttPublisher
    )
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _mqttPublisher = mqttPublisher;
    }

    public async Task<bool> ClearAllMaps(CancellationToken cancellationToken = default)
    {
        var mqttClient = await _connectionManager.GetClient(cancellationToken);

        var payload = new ClearMapsPayload();

        _logger.LogInformation("Publishing empty pmaps to '{Topic}' to clear all persistent maps", RoombaTopic.Delta);

        var success = await _mqttPublisher.PublishAsync(
            mqttClient,
            RoombaTopic.Delta,
            payload,
            RoombaJsonContext.Default.ClearMapsPayload,
            cancellationToken
        );

        if (success)
        {
            _logger.LogInformation("All persistent maps cleared successfully");
        }
        else
        {
            _logger.LogError("Failed to clear persistent maps");
        }

        return success;
    }
}
