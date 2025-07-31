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
        
        services.AddAndValidateServiceOptions<RoombaSettings>(configuration);

        services.TryAddSingleton<MqttClientFactory>();

        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaClient, RoombaClient>();
        services.TryAddSingleton<IRoombaSubscriber, RoombaSubscriber>();

        return services;
    }

    public static IServiceCollection AddAndValidateServiceOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration
    ) where T : class
    {
        var sectionName = typeof(T).Name;
        var section = configuration.GetSection(sectionName);

        var configValue = section.Get<T>();
        if (configValue is null)
        {
            throw new InvalidOperationException($"Configuration section '{sectionName}' is required.");
        }

        services.TryAddSingleton(configValue);

        services.AddOptions<T>()
            .Configure(options => section.Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
