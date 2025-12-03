using MQTTnet;

namespace RoombaNet.Core.WifiConfig;

/// <summary>
/// Represents a single WiFi configuration step that can be executed.
/// </summary>
public interface IWifiConfigCommand
{
    /// <summary>
    /// Gets the MQTT topic this command publishes to.
    /// </summary>
    string Topic { get; }

    /// <summary>
    /// Gets a description of what this command does (for logging).
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the WiFi configuration command.
    /// </summary>
    /// <param name="client">The MQTT client to use for publishing.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(IMqttClient client, CancellationToken cancellationToken = default);
}
