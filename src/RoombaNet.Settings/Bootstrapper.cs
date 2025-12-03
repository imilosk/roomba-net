using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Settings;

public static class Bootstrapper
{
    /// <summary>
    /// Registers RoombaSettings from configuration.
    /// This should be called once at the application startup before any other service registration.
    /// </summary>
    public static IServiceCollection AddRoombaSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection(nameof(RoombaSettings));
        var roombaSettings = configurationSection.Get<RoombaSettings>();

        services.Configure<RoombaSettings>(configurationSection);
        services.AddSingleton(
            roombaSettings ??
            throw new InvalidOperationException($"{nameof(RoombaSettings)} configuration is missing or invalid.")
        );

        return services;
    }
}
