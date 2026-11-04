using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Proxy.Configuration;

/// <summary>
/// Builds the proxy's robot list from configuration. Prefers an explicit <c>Proxy:Robots</c> list and
/// otherwise derives one from <c>Robots:Profiles</c> (the same profile map the CLI and API use), reading
/// each referenced <see cref="RoombaSettings"/> section.
/// </summary>
public sealed class ConfigurationProxyRobotSource : IProxyRobotSource
{
    private readonly IConfiguration _configuration;
    private readonly ProxySettings _settings;
    private readonly ILogger<ConfigurationProxyRobotSource> _logger;

    public ConfigurationProxyRobotSource(
        IConfiguration configuration,
        ProxySettings settings,
        ILogger<ConfigurationProxyRobotSource> logger)
    {
        _configuration = configuration;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProxyRobotDescriptor>> GetRobotsAsync(CancellationToken cancellationToken = default)
    {
        var robots = _settings.Robots.Count > 0
            ? FromExplicitList()
            : FromProfiles();

        return Task.FromResult<IReadOnlyList<ProxyRobotDescriptor>>(robots);
    }

    private List<ProxyRobotDescriptor> FromExplicitList()
    {
        var result = new List<ProxyRobotDescriptor>();
        foreach (var robot in _settings.Robots)
        {
            if (string.IsNullOrWhiteSpace(robot.Blid) || string.IsNullOrWhiteSpace(robot.Ip))
            {
                _logger.LogWarning("Skipping Proxy:Robots entry with missing Blid/Ip");
                continue;
            }

            result.Add(new ProxyRobotDescriptor
            {
                Blid = robot.Blid,
                Ip = robot.Ip,
                Port = robot.Port,
                Password = robot.Password,
                UseTls = robot.UseTls,
            });
        }

        return result;
    }

    private List<ProxyRobotDescriptor> FromProfiles()
    {
        var result = new List<ProxyRobotDescriptor>();
        var profiles = _configuration.GetSection("Robots:Profiles").Get<Dictionary<string, string>>()
                       ?? new Dictionary<string, string>();

        foreach (var (profile, section) in profiles)
        {
            if (string.IsNullOrWhiteSpace(section))
            {
                continue;
            }

            var robot = _configuration.GetSection(section).Get<RoombaSettings>();
            if (robot is null || string.IsNullOrWhiteSpace(robot.Blid) || string.IsNullOrWhiteSpace(robot.Ip))
            {
                _logger.LogWarning("Skipping profile '{Profile}' -> section '{Section}': missing Blid/Ip", profile, section);
                continue;
            }

            result.Add(new ProxyRobotDescriptor
            {
                Blid = robot.Blid,
                Ip = robot.Ip,
                Port = robot.Port,
                Password = robot.Password,
                UseTls = true,
            });
        }

        return result;
    }
}
