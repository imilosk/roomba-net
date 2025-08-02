using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Transport.Mqtt;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddMqtt(configuration);
        services.AddPasswordClient(configuration);

        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaCommandClient, RoombaCommandClient>();
        services.TryAddSingleton<IRoombaSettingsClient, RoombaSettingsClient>();
        services.TryAddSingleton<IRoombaSubscriber, RoombaSubscriber>();

        return services;
    }
}
