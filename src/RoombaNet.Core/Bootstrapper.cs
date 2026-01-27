using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddPasswordClient(configuration);

        services.TryAddSingleton<IRoombaDiscoveryService, RoombaDiscoveryService>();
        services.TryAddSingleton<IRoombaPasswordService, RoombaPasswordService>();

        return services;
    }
}
