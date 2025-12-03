using System.Text.Json.Serialization.Metadata;
using MQTTnet;

namespace RoombaNet.Core;

/// <summary>
/// Provides functionality to publish messages to MQTT topics.
/// </summary>
public interface IMqttPublisher
{
    /// <summary>
    /// Publishes a message payload to the specified MQTT topic.
    /// </summary>
    /// <typeparam name="T">The type of the payload to publish.</typeparam>
    /// <param name="mqttClient">The MQTT client to use for publishing.</param>
    /// <param name="topic">The MQTT topic to publish to.</param>
    /// <param name="payload">The payload to serialize and publish.</param>
    /// <param name="jsonTypeInfo">The JSON type information for AOT serialization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the message was published successfully; otherwise, false.</returns>
    Task<bool> PublishAsync<T>(
        IMqttClient mqttClient,
        string topic,
        T payload,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default
    );
}
