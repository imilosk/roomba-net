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
    public static IServiceProvider CreateServiceProvider()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .AddJsonFile($"secrets.{environment}.json", true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        services.AddRoombaSettings(configuration);

        services.AddCore(configuration);

        services.AddMqtt(configuration);
        services.TryAddSingleton<IMqttPublisher, MqttPublisher>();
        services.TryAddSingleton<WifiConfigCommandBuilder>();
        services.TryAddSingleton<IRoombaConnectionManager, RoombaConnectionManager>();
        services.TryAddSingleton<IRoombaCommandService, RoombaCommandService>();
        services.TryAddSingleton<IRoombaSettingsService, RoombaSettingsService>();
        services.TryAddSingleton<IRoombaSubscriptionService, RoombaSubscriptionService>();
        services.TryAddSingleton<IRoombaWifiService, RoombaWifiService>();

        services.TryAddSingleton<OutputService>();
        services.TryAddSingleton<CliCommandBuilder>();

        return services.BuildServiceProvider();
    }
}
