using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet;
using RoombaNet.Proxy.Configuration;

namespace RoombaNet.Proxy;

/// <summary>
/// DI wiring for the multi-robot MQTT proxy.
/// </summary>
public static class Bootstrapper
{
    /// <summary>
    /// Registers proxy services and, when <c>Proxy:Enabled</c> is true, the <see cref="RoombaProxyService"/>
    /// hosted service. Robots are resolved from configuration unless another <see cref="IProxyRobotSource"/>
    /// has already been registered (e.g. one backed by the API robot registry).
    /// </summary>
    public static IServiceCollection AddRoombaProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ProxySettings.SectionName).Get<ProxySettings>() ?? new ProxySettings();
        services.TryAddSingleton(settings);
        services.TryAddSingleton<MqttClientFactory>();
        services.TryAddSingleton<IProxyRobotSource, ConfigurationProxyRobotSource>();
        services.TryAddSingleton<RoombaProxyService>();

        if (settings.Enabled)
        {
            services.AddHostedService(sp => sp.GetRequiredService<RoombaProxyService>());
        }

        return services;
    }
}
