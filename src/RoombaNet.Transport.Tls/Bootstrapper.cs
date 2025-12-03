using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoombaNet.Transport.Tls;

public static class Bootstrapper
{
    public static IServiceCollection AddPasswordClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IRoombaPasswordClient, RoombaPasswordClient>();
        services.TryAddSingleton<IRoombaDiscoveryClient, RoombaDiscoveryClient>();

        return services;
    }
}
