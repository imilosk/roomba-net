
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Channels;
using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public partial class RoombaStatusService : BackgroundService
{
    private readonly IRoombaSubscriber _subscriber;
    private readonly ILogger<RoombaStatusService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<RoombaStatusUpdate> _statusChannel;
    private RoombaStatusUpdate _lastStatusUpdate = new(string.Empty, default, DateTime.MinValue);

    [GeneratedRegex(@"^\$aws/things/.+/shadow/update$")]
    private static partial Regex ShadowUpdateTopicRegex();

    public RoombaStatusService(
        IRoombaSubscriber subscriber,
        ILogger<RoombaStatusService> logger,
        TimeProvider timeProvider)
    {
        _subscriber = subscriber;
        _logger = logger;
        _timeProvider = timeProvider;

        // Unbounded channel to handle bursts of status updates
        _statusChannel = Channel.CreateUnbounded<RoombaStatusUpdate>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true,
            });
    }

    public ChannelReader<RoombaStatusUpdate> StatusUpdates => _statusChannel.Reader;

    public RoombaStatusUpdate GetLastStatus()
    {
        return _lastStatusUpdate;
    }

    private static JsonElement DeepMergeJson(JsonElement target, JsonElement source)
    {
        if (target.ValueKind == JsonValueKind.Undefined || target.ValueKind == JsonValueKind.Null)
        {
            return source.Clone();
        }

        if (source.ValueKind != JsonValueKind.Object || target.ValueKind != JsonValueKind.Object)
        {
            return source.Clone();
        }

        var merged = new Dictionary<string, JsonElement>();

        // Copy all properties from target
        foreach (var property in target.EnumerateObject())
        {
            merged[property.Name] = property.Value.Clone();
        }

        // Merge or override with properties from source
        foreach (var property in source.EnumerateObject())
        {
            if (merged.TryGetValue(property.Name, out var existingValue))
            {
                // Recursively merge if both are objects
                merged[property.Name] = DeepMergeJson(existingValue, property.Value);
            }
            else
            {
                merged[property.Name] = property.Value.Clone();
            }
        }

        // Convert dictionary back to JsonElement
        var jsonString = JsonSerializer.Serialize(merged);
        return JsonDocument.Parse(jsonString).RootElement.Clone();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Roomba Status Service starting...");

        try
        {
            await _subscriber.Subscribe(messageEvent =>
            {
                var topic = messageEvent.ApplicationMessage.Topic;
                var payloadString = Encoding.UTF8.GetString(messageEvent.ApplicationMessage.Payload);
                var timestamp = _timeProvider.GetUtcNow().DateTime;

                JsonElement payloadJson;
                try
                {
                    payloadJson = JsonDocument.Parse(payloadString).RootElement.Clone();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse payload as JSON. Storing as raw string.");
                    payloadJson = JsonDocument.Parse($"\"{payloadString.Replace("\"", "\\\"")}\"").RootElement.Clone();
                }

                var statusUpdate = new RoombaStatusUpdate(topic, payloadJson, timestamp);

                if (ShadowUpdateTopicRegex().IsMatch(topic))
                {
                    var mergedPayload = DeepMergeJson(_lastStatusUpdate.Payload, payloadJson);
                    _lastStatusUpdate = new RoombaStatusUpdate(topic, mergedPayload, timestamp);
                }

                _statusChannel.Writer.TryWrite(statusUpdate);

                _logger.LogDebug("Received status update on topic '{Topic}'", topic);
            }, stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Roomba Status Service is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Roomba Status Service encountered an error and will stop.");
        }
        finally
        {
            _statusChannel.Writer.Complete();
            _logger.LogInformation("Roomba Status Service stopped.");
        }
    }
}
