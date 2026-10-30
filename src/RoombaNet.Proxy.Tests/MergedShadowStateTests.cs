using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoombaNet.Proxy.State;
using Xunit;

namespace RoombaNet.Proxy.Tests;

public class MergedShadowStateTests
{
    [Fact]
    public void TryMerge_DeepMerges_ReportedState()
    {
        var state = new MergedShadowState();

        Assert.True(state.TryMerge(Bytes("""{"state":{"reported":{"batPct":50,"bin":{"full":false}}}}""")));
        Assert.True(state.TryMerge(Bytes("""{"state":{"reported":{"bin":{"present":true},"cleanMissionStatus":{"phase":"run"}}}}""")));

        var snapshot = state.SnapshotShadowDocument();
        Assert.NotNull(snapshot);

        var root = JsonNode.Parse(snapshot!)!;
        var reported = root["state"]!["reported"]!;

        Assert.Equal(50, (int)reported["batPct"]!);
        // Nested object was deep-merged, not replaced.
        Assert.False((bool)reported["bin"]!["full"]!);
        Assert.True((bool)reported["bin"]!["present"]!);
        Assert.Equal("run", (string)reported["cleanMissionStatus"]!["phase"]!);
    }

    [Fact]
    public void TryMerge_IgnoresNonJsonPayload()
    {
        var state = new MergedShadowState();
        Assert.False(state.TryMerge(Bytes("not-json")));
        Assert.False(state.HasState);
        Assert.Null(state.SnapshotShadowDocument());
    }

    [Fact]
    public void TryMerge_IgnoresJsonWithoutReported()
    {
        var state = new MergedShadowState();
        Assert.False(state.TryMerge(Bytes("""{"foo":"bar"}""")));
        Assert.False(state.HasState);
    }

    [Fact]
    public void SnapshotReported_ReturnsMergedObject()
    {
        var state = new MergedShadowState();
        state.TryMerge(Bytes("""{"state":{"reported":{"name":"Rosie"}}}"""));

        var reported = state.SnapshotReported();
        Assert.NotNull(reported);
        using var doc = JsonDocument.Parse(reported!);
        Assert.Equal("Rosie", doc.RootElement.GetProperty("name").GetString());
    }

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);
}
