using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet;
using RoombaNet.Mqtt.Settings;

namespace RoombaNet.Mqtt;

public static class Bootstrapper
{
    public static IServiceCollection AddMqtt(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        var configurationSection = configuration.GetSection(nameof(RoombaSettings));
        var roombaSettings = configurationSection.Get<RoombaSettings>();
        services.Configure<RoombaSettings>(configurationSection);
        services.AddSingleton(roombaSettings ??
                              throw new InvalidOperationException($"{nameof(RoombaSettings)} configuration is missing or invalid."));

        services.TryAddSingleton<MqttClientFactory>();

        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaCommandClient, RoombaCommandClient>();
        services.TryAddSingleton<IRoombaSettingsClient, RoombaSettingsClient>();
        services.TryAddSingleton<IRoombaSubscriber, RoombaSubscriber>();

        return services;
    }
}
