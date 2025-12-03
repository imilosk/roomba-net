using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace RoombaNet.Core;

/// <summary>
/// Handles publishing of messages to MQTT topics with JSON serialization.
/// </summary>
public class MqttPublisher : IMqttPublisher
{
    private readonly ILogger<MqttPublisher> _logger;

    public MqttPublisher(ILogger<MqttPublisher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> PublishAsync<T>(
        IMqttClient mqttClient,
        string topic,
        T payload,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
                payload,
                jsonTypeInfo
            );

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonBytes)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            var publishResult = await mqttClient.PublishAsync(message, cancellationToken);

            if (publishResult.IsSuccess)
            {
                _logger.LogDebug("Successfully published message to topic '{Topic}'", topic);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to publish message to topic '{Topic}': {ReasonCode}",
                    topic,
                    publishResult.ReasonCode
                );
            }

            return publishResult.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to topic '{Topic}'", topic);
            return false;
        }
    }
}
