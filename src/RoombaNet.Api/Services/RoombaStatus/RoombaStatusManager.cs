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
        var payload = messageEvent.ApplicationMessage.ConvertPayloadToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        var lastStateJson = WrapStateForMerging(state.LastStatusUpdate.State);
        var mergedPayload = DeepMergeJson(lastStateJson, payload);

        var updatedState = UnwrapStateFromJson(mergedPayload);
        var statusUpdate = new RoombaStatusUpdate(updatedState, timestamp);

        state.LastStatusUpdate = statusUpdate;
        state.UpdatesChannel.Writer.TryWrite(statusUpdate);
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
