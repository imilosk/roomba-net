using RoombaNet.Core;
using RoombaNet.Transport.Mqtt;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Api.Services.RoombaClients;

public sealed class RoombaClient : IAsyncDisposable, IDisposable
{
    public RoombaClient(
        string robotId,
        RoombaSettings settings,
        IRoombaCommandService commandService,
        IRoombaSettingsService settingsService,
        IRoombaSubscriptionService subscriptionService,
        IRoombaConnectionManager connectionManager)
    {
        RobotId = robotId;
        Settings = settings;
        Commands = commandService;
        SettingsService = settingsService;
        Subscriptions = subscriptionService;
        ConnectionManager = connectionManager;
        LastUsedUtc = DateTime.UtcNow;
    }

    public string RobotId { get; }
    public RoombaSettings Settings { get; }
    public IRoombaCommandService Commands { get; }
    public IRoombaSettingsService SettingsService { get; }
    public IRoombaSubscriptionService Subscriptions { get; }
    public IRoombaConnectionManager ConnectionManager { get; }
    public DateTime LastUsedUtc { get; private set; }

    public void Touch() => LastUsedUtc = DateTime.UtcNow;

    public void Dispose()
    {
        ConnectionManager.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await ConnectionManager.DisposeAsync();
    }
}
