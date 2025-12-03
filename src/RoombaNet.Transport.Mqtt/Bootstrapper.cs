using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet;

namespace RoombaNet.Transport.Mqtt;

public static class Bootstrapper
{
    public static IServiceCollection AddMqtt(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<MqttClientFactory>();

        return services;
    }
}
