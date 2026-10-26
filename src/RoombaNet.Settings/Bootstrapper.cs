using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Settings;

public static class Bootstrapper
{
    public static IServiceCollection AddRoombaSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = nameof(RoombaSettings)
    )
    {
        var configurationSection = configuration.GetSection(sectionName);
        var roombaSettings = configurationSection.Get<RoombaSettings>();

        services.Configure<RoombaSettings>(configurationSection);
        if (roombaSettings is not null && !string.IsNullOrWhiteSpace(roombaSettings.Ip))
        {
            services.AddSingleton(roombaSettings);
        }
        else
        {
            throw new InvalidOperationException(
                $"Robot settings section '{sectionName}' is missing or invalid (no Ip). " +
                "Check your secrets file and any --robot profile mapping.");
        }

        return services;
    }
}
