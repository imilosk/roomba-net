using System.Collections.Concurrent;
using MQTTnet;
using RoombaNet.Api.Services.RobotRegistry;
using RoombaNet.Core;
using RoombaNet.Settings.Settings;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Api.Services.RoombaClients;

public class RoombaClientFactory : IRoombaClientFactory
{
    private readonly IRobotRegistry _registry;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, RoombaClient> _clients = new();
    private readonly object _sync = new();

    public RoombaClientFactory(
        IRobotRegistry registry,
        MqttClientFactory mqttClientFactory,
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider)
    {
        _registry = registry;
        _mqttClientFactory = mqttClientFactory;
        _loggerFactory = loggerFactory;
        _timeProvider = timeProvider;
    }

    public async Task<RoombaClient> GetClient(string robotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(robotId))
        {
            throw new ArgumentException("robotId is required.", nameof(robotId));
        }

        var credentials = await _registry.GetCredentials(robotId, cancellationToken);
        if (credentials is null)
        {
            throw new InvalidOperationException($"Robot '{robotId}' not found or missing credentials.");
        }

        var settings = new RoombaSettings
        {
            Blid = credentials.Blid,
            Ip = credentials.Ip,
            Port = credentials.Port,
            Password = credentials.Password,
        };

        lock (_sync)
        {
            if (_clients.TryGetValue(robotId, out var existing) && SettingsEqual(existing.Settings, settings))
            {
                existing.Touch();
                return existing;
            }

            if (existing is not null)
            {
                existing.Dispose();
            }

            var created = CreateClient(robotId, settings);
            _clients[robotId] = created;
            return created;
        }
    }

    public bool RemoveClient(string robotId)
    {
        if (_clients.TryRemove(robotId, out var client))
        {
            client.Dispose();
            return true;
        }

        return false;
    }

    private RoombaClient CreateClient(string robotId, RoombaSettings settings)
    {
        var connectionManager = new RoombaConnectionManager(
            _loggerFactory.CreateLogger<RoombaConnectionManager>(),
            _mqttClientFactory,
            settings
        );

        var publisher = new MqttPublisher(_loggerFactory.CreateLogger<MqttPublisher>());

        var commandService = new RoombaCommandService(
            _loggerFactory.CreateLogger<RoombaCommandService>(),
            connectionManager,
            publisher,
            _timeProvider
        );

        var settingsService = new RoombaSettingsService(
            _loggerFactory.CreateLogger<RoombaSettingsService>(),
            connectionManager,
            publisher
        );

        var subscriptionService = new RoombaSubscriptionService(
            connectionManager,
            _loggerFactory.CreateLogger<RoombaSubscriptionService>()
        );

        return new RoombaClient(
            robotId,
            settings,
            commandService,
            settingsService,
            subscriptionService,
            connectionManager
        );
    }

    private static bool SettingsEqual(RoombaSettings left, RoombaSettings right)
    {
        return string.Equals(left.Ip, right.Ip, StringComparison.Ordinal)
               && left.Port == right.Port
               && string.Equals(left.Blid, right.Blid, StringComparison.Ordinal)
               && string.Equals(left.Password, right.Password, StringComparison.Ordinal);
    }
}
