
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public class RoombaStatusService : BackgroundService
{
    private readonly IRoombaSubscriber _subscriber;
    private readonly ILogger<RoombaStatusService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<RoombaStatusUpdate> _statusChannel;
    private readonly RoombaStatusUpdate _lastStatusUpdate = new("{}", DateTime.MinValue);

    public RoombaStatusService(
        IRoombaSubscriber subscriber,
        ILogger<RoombaStatusService> logger,
        TimeProvider timeProvider)
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

        foreach (var property in target.EnumerateObject())
        {
            merged[property.Name] = property.Value.Clone();
        }

        foreach (var property in source.EnumerateObject())
        {
            if (merged.TryGetValue(property.Name, out var existingValue) &&
                property.Value.ValueKind == JsonValueKind.Object &&
                existingValue.ValueKind == JsonValueKind.Object)
            {
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

                var payload = Encoding.UTF8.GetString(messageEvent.ApplicationMessage.Payload);
                var timestamp = _timeProvider.GetUtcNow().DateTime;

                var mergedPayload = DeepMergeJson(_lastStatusUpdate.Payload, payload);

                _logger.LogDebug("Received message on topic {Payload}", mergedPayload);

                _lastStatusUpdate.Payload = mergedPayload;
                _lastStatusUpdate.Timestamp = timestamp;

                _statusChannel.Writer.TryWrite(_lastStatusUpdate);

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
