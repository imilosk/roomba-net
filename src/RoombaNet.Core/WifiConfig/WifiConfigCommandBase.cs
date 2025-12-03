using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using MQTTnet;
using RoombaNet.Core.Payloads;

namespace RoombaNet.Core.WifiConfig;

/// <summary>
/// Base class for WiFi configuration commands that provides common publishing functionality.
/// </summary>
public abstract class WifiConfigCommandBase<TState> : IWifiConfigCommand where TState : class
{
    private readonly ILogger _logger;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly JsonTypeInfo<WifiConfigMessage<TState>> _jsonTypeInfo;

    protected WifiConfigCommandBase(
        ILogger logger,
        IMqttPublisher mqttPublisher,
        string topic,
        string description,
        JsonTypeInfo<WifiConfigMessage<TState>> jsonTypeInfo)
    {
        _logger = logger;
        _mqttPublisher = mqttPublisher;
        Topic = topic;
        Description = description;
        _jsonTypeInfo = jsonTypeInfo;
    }

    public string Topic { get; }
    public string Description { get; }

    /// <summary>
    /// Creates the state object for this command.
    /// </summary>
    protected abstract TState CreateState();

    public async Task ExecuteAsync(IMqttClient client, CancellationToken cancellationToken = default)
    {
        var state = CreateState();
        var message = new WifiConfigMessage<TState>
        {
            State = state
        };

        await _mqttPublisher.PublishAsync(client, Topic, message, _jsonTypeInfo, cancellationToken);

        _logger.LogDebug("Executed command: {Description}", Description);
    }
}
