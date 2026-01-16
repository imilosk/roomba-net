using Microsoft.Extensions.DependencyInjection.Extensions;
using RoombaNet.Api.Services.Secrets;
using RoombaNet.Api.Settings;

namespace RoombaNet.Api.Services.RobotRegistry;

public static class Bootstrapper
{
    public static IServiceCollection AddRobotRegistry(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RobotRegistry").Get<RobotRegistrySettings>() ?? new RobotRegistrySettings();
        services.AddSingleton(settings);

        services.TryAddSingleton<ISecretProtector>(_ =>
        {
            if (string.IsNullOrWhiteSpace(settings.EncryptionKey))
            {
                return new NoOpSecretProtector();
            }

            var key = Convert.FromBase64String(settings.EncryptionKey);
            return new AesGcmSecretProtector(key);
        });

        services.TryAddSingleton<IRobotRegistry, SqliteRobotRegistry>();

        return services;
    }
}
