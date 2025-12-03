using System.Text.Json;
using System.Threading.Channels;
using MQTTnet;
using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public class RoombaStatusService : BackgroundService
{
    private const string JsonStateProperty = "state";
    private const string JsonReportedProperty = "reported";

    private readonly IRoombaSubscriptionService _subscriber;
    private readonly ILogger<RoombaStatusService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<RoombaStatusUpdate> _statusChannel;
    private RoombaStatusUpdate _lastStatusUpdate = new(new RoombaState(), DateTime.MinValue);

    public RoombaStatusService(
        IRoombaSubscriptionService subscriber,
        ILogger<RoombaStatusService> logger,
        TimeProvider timeProvider
    )
    {
        _subscriber = subscriber;
        _logger = logger;
        _timeProvider = timeProvider;

        _statusChannel = Channel.CreateUnbounded<RoombaStatusUpdate>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true,
            });
    }

    public ChannelReader<RoombaStatusUpdate> StatusUpdates => _statusChannel.Reader;

    public RoombaStatusUpdate GetLastStatus() => _lastStatusUpdate;

    private static string WrapStateForMerging(RoombaState state)
    {
        var wrapped = new
        {
            state = new
            {
                reported = state,
            },
        };
        return JsonSerializer.Serialize(wrapped);
    }

    private static RoombaState UnwrapStateFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var reportedElement = doc.RootElement
            .GetProperty(JsonStateProperty)
            .GetProperty(JsonReportedProperty);
        var reportedJson = JsonSerializer.Serialize(reportedElement);

        return JsonSerializer.Deserialize<RoombaState>(reportedJson) ?? new RoombaState();
    }

    private static string DeepMergeJson(string targetJson, string sourceJson)
    {
        if (string.IsNullOrWhiteSpace(targetJson) || targetJson == "{}")
            return sourceJson;

        using var targetDoc = JsonDocument.Parse(targetJson);
        using var sourceDoc = JsonDocument.Parse(sourceJson);

        var target = targetDoc.RootElement;
        var source = sourceDoc.RootElement;

        if (source.ValueKind != JsonValueKind.Object || target.ValueKind != JsonValueKind.Object)
            return sourceJson;

        var merged = BuildMergedDictionary(target, source);

        return JsonSerializer.Serialize(merged);
    }

    private static Dictionary<string, JsonElement> BuildMergedDictionary(JsonElement target, JsonElement source)
    {
        var merged = new Dictionary<string, JsonElement>();

        foreach (var property in target.EnumerateObject())
        {
            merged[property.Name] = property.Value.Clone();
        }

        foreach (var property in source.EnumerateObject())
        {
            if (ShouldMergeNestedObject(merged, property))
            {
                merged[property.Name] = MergeNestedObjects(merged[property.Name], property.Value);
            }
            else
            {
                merged[property.Name] = property.Value.Clone();
            }
        }

        return merged;
    }

    private static bool ShouldMergeNestedObject(Dictionary<string, JsonElement> merged, JsonProperty property)
    {
        return merged.TryGetValue(property.Name, out var existingValue)
               && property.Value.ValueKind == JsonValueKind.Object
               && existingValue.ValueKind == JsonValueKind.Object;
    }

    private static JsonElement MergeNestedObjects(JsonElement existing, JsonElement incoming)
    {
        var existingJson = JsonSerializer.Serialize(existing);
        var newJson = JsonSerializer.Serialize(incoming);
        var mergedJson = DeepMergeJson(existingJson, newJson);

        return JsonDocument.Parse(mergedJson).RootElement.Clone();
    }

    private void ProcessMessage(MqttApplicationMessageReceivedEventArgs messageEvent)
    {
        var topic = messageEvent.ApplicationMessage.Topic;
        var payload = messageEvent.ApplicationMessage.ConvertPayloadToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        var lastStateJson = WrapStateForMerging(_lastStatusUpdate.State);
        var mergedPayload = DeepMergeJson(lastStateJson, payload);

        _logger.LogDebug("Received message on topic {Payload}", mergedPayload);

        var state = UnwrapStateFromJson(mergedPayload);
        var statusUpdate = new RoombaStatusUpdate(state, timestamp);

        _lastStatusUpdate = statusUpdate;
        _statusChannel.Writer.TryWrite(statusUpdate);

        _logger.LogDebug("Received status update on topic '{Topic}'", topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Roomba Status Service starting...");

        try
        {
            await _subscriber.Subscribe(ProcessMessage, stoppingToken);
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