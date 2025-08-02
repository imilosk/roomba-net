using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Transport.Tls;

public static class Bootstrapper
{
    public static IServiceCollection AddPasswordClient(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection(nameof(RoombaSettings));
        var roombaSettings = configurationSection.Get<RoombaSettings>();
        services.Configure<RoombaSettings>(configurationSection);
        services.AddSingleton(
            roombaSettings ??
            throw new InvalidOperationException($"{nameof(RoombaSettings)} configuration is missing or invalid.")
        );

        services.TryAddSingleton<IRoombaPasswordClient, RoombaPasswordClient>();

        return services;
    }
}
