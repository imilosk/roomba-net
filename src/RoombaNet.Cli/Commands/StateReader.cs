using System.Text.Json;
using System.Text.Json.Nodes;
using MQTTnet;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

/// <summary>
/// Subscribes to the robot and accumulates the reported shadow state into a single
/// merged JSON object, returning once a predicate is satisfied or a timeout elapses.
/// Uses the mutable JSON DOM (JsonNode) so it stays AOT-compatible.
/// </summary>
public static class StateReader
{
    public static async Task<JsonObject> ReadState(
        IRoombaSubscriptionService subscriber,
        Func<JsonObject, bool> isComplete,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var accumulator = new JsonObject();
        var completion = new TaskCompletionSource();
        var sync = new object();

        await subscriber.Subscribe(e =>
        {
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            try
            {
                var node = JsonNode.Parse(payload);
                var reported = node?["state"]?["reported"] as JsonObject ?? node as JsonObject;
                if (reported is null)
                {
                    return;
                }

                lock (sync)
                {
                    foreach (var property in reported)
                    {
                        accumulator[property.Key] = property.Value?.DeepClone();
                    }

                    if (isComplete(accumulator))
                    {
                        completion.TrySetResult();
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore non-JSON messages.
            }
        }, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        await using (cts.Token.Register(() => completion.TrySetResult()))
        {
            await completion.Task;
        }

        lock (sync)
        {
            return (JsonObject)accumulator.DeepClone();
        }
    }
}
