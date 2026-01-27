using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Settings.Settings;
using RoombaNet.Transport.Mqtt;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        IConfiguration configuration,
        bool requireSettings = true)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddPasswordClient(configuration);

        var hasSettings = configuration.GetSection(nameof(RoombaSettings)).Get<RoombaSettings>() is not null;
        if (requireSettings || hasSettings)
        {
            services.AddMqtt(configuration);
            services.TryAddSingleton<IMqttPublisher, MqttPublisher>();
            services.TryAddSingleton<WifiConfig.WifiConfigCommandBuilder>();
            services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
            services.TryAddSingleton<IRoombaCommandService, RoombaCommandService>();
            services.TryAddSingleton<IRoombaSettingsService, RoombaSettingsService>();
            services.TryAddSingleton<IRoombaSubscriptionService, RoombaSubscriptionService>();
            services.TryAddSingleton<IRoombaWifiService, RoombaWifiService>();
        }
        services.TryAddSingleton<IRoombaDiscoveryService, RoombaDiscoveryService>();
        services.TryAddSingleton<IRoombaPasswordService, RoombaPasswordService>();

        return services;
    }
}
