using System.Text.Json;
using System.Threading.Channels;
using MQTTnet;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services.RoombaClients;

namespace RoombaNet.Api.Services.RoombaStatus;

public class RoombaStatusManager
{
    private const string JsonStateProperty = "state";
    private const string JsonReportedProperty = "reported";

    private readonly IRoombaClientFactory _clientFactory;
    private readonly ILogger<RoombaStatusManager> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, RobotStatusState> _states = new();
    private readonly object _sync = new();

    public RoombaStatusManager(
        IRoombaClientFactory clientFactory,
        ILogger<RoombaStatusManager> logger,
        TimeProvider timeProvider)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<RoombaStatusSubscription> GetSubscription(
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var state = await EnsureSubscribed(robotId, cancellationToken);
        return new RoombaStatusSubscription(state.LastStatusUpdate, state.UpdatesChannel.Reader);
    }

    // The robot streams its full shadow across several retained messages right after we
    // subscribe. Wait a short settle window so they merge before returning a snapshot,
    // otherwise the first read is partial (missing pmaps/bin/battery, etc.).
    private static readonly TimeSpan SettleWindow = TimeSpan.FromMilliseconds(1800);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Returns the latest known robot state. Waits up to <paramref name="timeout"/> for the first
    /// status message if none has arrived yet, then a short settle window for the remaining
    /// retained shadow messages to merge in. Reads the accumulated snapshot directly rather than
    /// consuming the updates channel (which would steal events from SSE subscribers).
    /// </summary>
    public async Task<RoombaState> GetCurrentState(
        string robotId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var state = await EnsureSubscribed(robotId, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Wait until the first message has been received.
            while (state.LastStatusUpdate.Timestamp == DateTime.MinValue)
            {
                await Task.Delay(PollInterval, cts.Token);
            }

            // Let the remaining retained shadow messages merge into the snapshot.
            await Task.Delay(SettleWindow, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (state.LastStatusUpdate.Timestamp == DateTime.MinValue)
            {
                _logger.LogWarning("Timed out waiting for first status update from {RobotId}", robotId);
            }
        }

        return state.LastStatusUpdate.State;
    }

    private async Task<RobotStatusState> EnsureSubscribed(string robotId, CancellationToken cancellationToken)
    {
        RobotStatusState state;
        lock (_sync)
        {
            if (!_states.TryGetValue(robotId, out state!))
            {
                state = new RobotStatusState();
                _states[robotId] = state;
            }
        }

        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!state.IsSubscribed)
            {
                var client = await _clientFactory.GetClient(robotId, cancellationToken);
                await client.Subscriptions.Subscribe(
                    message => ProcessMessage(state, message),
                    cancellationToken
                );

                // Ask the robot to emit its full shadow now so snapshots are complete quickly
                // (it only pushes the full shadow periodically otherwise).
                await client.ConnectionManager.RequestShadow(cancellationToken);

                state.IsSubscribed = true;
                _logger.LogInformation("Subscribed to Roomba status for {RobotId}", robotId);
            }
        }
        finally
        {
            state.Gate.Release();
        }

        return state;
    }

    private void ProcessMessage(RobotStatusState state, MqttApplicationMessageReceivedEventArgs messageEvent)
    {
        var payload = messageEvent.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var lastStateJson = WrapStateForMerging(state.LastStatusUpdate.State);
            var mergedPayload = DeepMergeJson(lastStateJson, payload);

            var updatedState = UnwrapStateFromJson(mergedPayload);
            var statusUpdate = new RoombaStatusUpdate(updatedState, timestamp);

            state.LastStatusUpdate = statusUpdate;
            state.UpdatesChannel.Writer.TryWrite(statusUpdate);
        }
        catch (Exception ex)
        {
            // A single unmappable field must not drop the whole shadow and leave the
            // snapshot permanently partial. Log it and keep the last good state.
            _logger.LogWarning(ex, "Failed to merge status message on topic {Topic}; keeping last known state",
                messageEvent.ApplicationMessage.Topic);
        }
    }

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

    private sealed class RobotStatusState
    {
        public Channel<RoombaStatusUpdate> UpdatesChannel { get; } = global::System.Threading.Channels.Channel
            .CreateUnbounded<RoombaStatusUpdate>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true,
            });

        public RoombaStatusUpdate LastStatusUpdate { get; set; } = new(new RoombaState(), DateTime.MinValue);
        public bool IsSubscribed { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
