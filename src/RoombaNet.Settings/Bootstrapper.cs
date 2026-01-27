using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Settings;

public static class Bootstrapper
{
    public static IServiceCollection AddRoombaSettings(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var configurationSection = configuration.GetSection(nameof(RoombaSettings));
        var roombaSettings = configurationSection.Get<RoombaSettings>();

        services.Configure<RoombaSettings>(configurationSection);
        if (roombaSettings is not null)
        {
            services.AddSingleton(roombaSettings);
        }
        else
        {
            throw new InvalidOperationException($"{nameof(RoombaSettings)} configuration is missing or invalid.");
        }

        return services;
    }
}
