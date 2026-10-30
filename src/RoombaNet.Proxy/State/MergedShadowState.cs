using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RoombaNet.Proxy.State;

/// <summary>
/// Accumulates the deep-merged <c>state.reported</c> document for a single robot.
/// The robot only emits the full shadow on connect and sends partial deltas afterwards, so the proxy
/// keeps the merged view and replays it (retained) to late-joining clients.
/// </summary>
public sealed class MergedShadowState
{
    private readonly object _sync = new();
    private readonly JsonObject _reported = new();
    private bool _hasState;

    /// <summary>Whether any reported state has been merged yet.</summary>
    public bool HasState
    {
        get
        {
            lock (_sync)
            {
                return _hasState;
            }
        }
    }

    /// <summary>
    /// Attempts to deep-merge the <c>state.reported</c> object from a raw robot payload.
    /// Non-JSON or non-matching payloads are ignored safely. Returns true when the merged state changed.
    /// </summary>
    public bool TryMerge(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return false;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(payload.ToArray());
        }
        catch (JsonException)
        {
            return false;
        }

        if (root is not JsonObject obj
            || obj["state"] is not JsonObject state
            || state["reported"] is not JsonObject reported)
        {
            return false;
        }

        lock (_sync)
        {
            DeepMerge(_reported, reported);
            _hasState = true;
        }

        return true;
    }

    /// <summary>
    /// Serialises the current merged state as a full shadow document: <c>{"state":{"reported":{...}}}</c>.
    /// Returns null when no state has been merged yet.
    /// </summary>
    public byte[]? SnapshotShadowDocument()
    {
        lock (_sync)
        {
            if (!_hasState)
            {
                return null;
            }

            var document = new JsonObject
            {
                ["state"] = new JsonObject
                {
                    ["reported"] = _reported.DeepClone(),
                },
            };

            return Encoding.UTF8.GetBytes(document.ToJsonString());
        }
    }

    /// <summary>Serialises just the merged reported object. Returns null when no state has been merged yet.</summary>
    public byte[]? SnapshotReported()
    {
        lock (_sync)
        {
            return _hasState ? Encoding.UTF8.GetBytes(_reported.ToJsonString()) : null;
        }
    }

    private static void DeepMerge(JsonObject destination, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (value is JsonObject sourceChild && destination[key] is JsonObject destChild)
            {
                DeepMerge(destChild, sourceChild);
            }
            else
            {
                destination[key] = value?.DeepClone();
            }
        }
    }
}
