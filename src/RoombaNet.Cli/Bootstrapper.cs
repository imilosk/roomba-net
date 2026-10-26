using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RoombaNet.Cli.Commands;
using RoombaNet.Cli.Services;
using RoombaNet.Core;
using RoombaNet.Core.WifiConfig;
using RoombaNet.Settings;
using RoombaNet.Transport.Mqtt;

namespace RoombaNet.Cli;

public static class Bootstrapper
{
    public static IServiceProvider CreateServiceProvider(string? robot = null)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .AddJsonFile($"secrets.{environment}.json", true)
            .AddEnvironmentVariables()
            .Build();

        var sectionName = ResolveRobotSection(configuration, robot);

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        services.AddRoombaSettings(configuration, sectionName);

        services.AddCore(configuration);

        services.AddMqtt(configuration);
        services.TryAddSingleton<IMqttPublisher, MqttPublisher>();
        services.TryAddSingleton<WifiConfigCommandBuilder>();
        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaCommandService, RoombaCommandService>();
        services.TryAddSingleton<IRoombaSettingsService, RoombaSettingsService>();
        services.TryAddSingleton<IRoombaMapService, RoombaMapService>();
        services.TryAddSingleton<IRoombaSubscriptionService, RoombaSubscriptionService>();
        services.TryAddSingleton<IRoombaWifiService, RoombaWifiService>();

        services.TryAddSingleton<OutputService>();
        services.TryAddSingleton<CliCommandBuilder>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Resolves which configuration section holds the robot's connection settings.
    /// A friendly profile name (from --robot or Robots:DefaultRobot) is looked up in the
    /// Robots:Profiles map; falls back to the literal name, then to "RoombaSettings".
    /// </summary>
    private static string ResolveRobotSection(IConfiguration configuration, string? robot)
    {
        const string defaultSection = nameof(RoombaNet.Settings.Settings.RoombaSettings);

        var profiles = configuration.GetSection("Robots:Profiles").Get<Dictionary<string, string>>()
                       ?? new Dictionary<string, string>();

        var name = robot ?? configuration["Robots:DefaultRobot"];
        if (string.IsNullOrWhiteSpace(name))
        {
            return defaultSection;
        }

        if (profiles.TryGetValue(name, out var section) && !string.IsNullOrWhiteSpace(section))
        {
            return section;
        }

        // Allow passing a literal config section name directly; else fall back.
        return configuration.GetSection(name).Exists() ? name : defaultSection;
    }
}
