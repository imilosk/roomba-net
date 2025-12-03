using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Settings;
using RoombaNet.Transport.Mqtt;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Register settings first - required by transport layers
        services.AddRoombaSettings(configuration);

        services.AddMqtt(configuration);
        services.AddPasswordClient(configuration);

        services.TryAddSingleton<IMqttPublisher, MqttPublisher>();
        services.TryAddSingleton<WifiConfig.WifiConfigCommandBuilder>();
        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaCommandService, RoombaCommandService>();
        services.TryAddSingleton<IRoombaSettingsService, RoombaSettingsService>();
        services.TryAddSingleton<IRoombaSubscriptionService, RoombaSubscriptionService>();
        services.TryAddSingleton<IRoombaWifiService, RoombaWifiService>();
        services.TryAddSingleton<IRoombaDiscoveryService, RoombaDiscoveryService>();
        services.TryAddSingleton<IRoombaPasswordService, RoombaPasswordService>();

        return services;
    }
}
