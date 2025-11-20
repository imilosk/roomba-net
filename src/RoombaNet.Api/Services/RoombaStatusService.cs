
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
    private RoombaStatusUpdate _lastStatusUpdate = new(string.Empty, "{}", DateTime.MinValue);

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

    private static string DeepMergeJson(string targetJson, string sourceJson)
    {
        if (string.IsNullOrWhiteSpace(targetJson) || targetJson == "{}")
        {
            return sourceJson;
        }

        using var targetDoc = JsonDocument.Parse(targetJson);
        using var sourceDoc = JsonDocument.Parse(sourceJson);

        var target = targetDoc.RootElement;
        var source = sourceDoc.RootElement;

        if (source.ValueKind != JsonValueKind.Object || target.ValueKind != JsonValueKind.Object)
        {
            return sourceJson;
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
            if (merged.TryGetValue(property.Name, out var existingValue) &&
                property.Value.ValueKind == JsonValueKind.Object &&
                existingValue.ValueKind == JsonValueKind.Object)
            {
                // Recursively merge if both are objects
                var existingJson = JsonSerializer.Serialize(existingValue);
                var newJson = JsonSerializer.Serialize(property.Value);
                var mergedJson = DeepMergeJson(existingJson, newJson);
                merged[property.Name] = JsonDocument.Parse(mergedJson).RootElement.Clone();
            }
            else
            {
                merged[property.Name] = property.Value.Clone();
            }
        }

        return JsonSerializer.Serialize(merged);
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

                string payloadJson;
                try
                {
                    // Validate it's valid JSON
                    using var testDoc = JsonDocument.Parse(payloadString);
                    payloadJson = payloadString;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse payload as JSON. Storing as escaped string.");
                    payloadJson = JsonSerializer.Serialize(payloadString);
                }

                var statusUpdate = new RoombaStatusUpdate(topic, payloadJson, timestamp);

                if (ShadowUpdateTopicRegex().IsMatch(topic))
                {
                    var mergedPayload = DeepMergeJson(_lastStatusUpdate.PayloadJson, payloadJson);
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
