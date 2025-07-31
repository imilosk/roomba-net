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

        var roombaSettings = new RoombaSettings();
        configuration.Bind(roombaSettings);
        services.TryAddSingleton(roombaSettings);

        services.TryAddSingleton<MqttClientFactory>();

        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaClient, RoombaClient>();
        services.TryAddSingleton<IRoombaSubscriber, RoombaSubscriber>();

        return services;
    }
}
