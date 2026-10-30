using System.Text.Json;
using Xunit;

namespace RoombaNet.Proxy.Tests;

[Collection("proxy-integration")]
public class ProxyIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Rejects_UnknownAndWrongCredentials_AcceptsCorrect()
    {
        await using var harness = new ProxyTestHarness();
        await harness.AddRobotAsync("blidA", "passA");
        await harness.StartAsync();

        // Correct credentials succeed.
        var ok = await harness.ConnectNativeAsync("blidA", "passA");
        Assert.True(ok.Client.IsConnected);

        // Wrong password is rejected.
        await Assert.ThrowsAnyAsync<Exception>(() => harness.ConnectNativeAsync("blidA", "wrong"));

        // Unknown BLID is rejected.
        await Assert.ThrowsAnyAsync<Exception>(() => harness.ConnectNativeAsync("blidUnknown", "passA"));
    }

    [Fact]
    public async Task Relays_OnlyAllowlistedTopics_FromClientToRobot()
    {
        await using var harness = new ProxyTestHarness();
        var robot = await harness.AddRobotAsync("blidA", "passA");
        await harness.StartAsync();

        var client = await harness.ConnectNativeAsync("blidA", "passA");

        await client.PublishAsync("cmd", """{"command":"start"}""");
        await client.PublishAsync("not-allowed", "nope");

        await ProxyTestHarness.WaitUntilAsync(() => robot.ReceivedTopic("cmd"), Timeout);
        // Give any (incorrect) relay of the disallowed topic a chance to arrive before asserting absence.
        await Task.Delay(300);

        Assert.True(robot.ReceivedTopic("cmd"));
        Assert.False(robot.ReceivedTopic("not-allowed"));
    }

    [Fact]
    public async Task IsolatesRobots_ClientAneverSeesRobotBsNativeTopics()
    {
        await using var harness = new ProxyTestHarness();
        var robotA = await harness.AddRobotAsync("blidA", "passA");
        var robotB = await harness.AddRobotAsync("blidB", "passB");
        await harness.StartAsync();

        var clientA = await harness.ConnectNativeAsync("blidA", "passA");
        var clientB = await harness.ConnectNativeAsync("blidB", "passB");
        await clientA.SubscribeAsync("#");
        await clientB.SubscribeAsync("#");

        await robotA.EmitAsync("wifistat", "from-A");
        await robotB.EmitAsync("wifistat", "from-B");

        await ProxyTestHarness.WaitUntilAsync(() => clientA.Has("wifistat") && clientB.Has("wifistat"), Timeout);
        await Task.Delay(300);

        // Each client sees only its own robot's payloads on the shared native topic.
        Assert.All(clientA.Messages.Where(m => m.Topic == "wifistat"), m => Assert.Equal("from-A", m.Payload));
        Assert.All(clientB.Messages.Where(m => m.Topic == "wifistat"), m => Assert.Equal("from-B", m.Payload));
        Assert.DoesNotContain(clientA.Messages, m => m.Payload == "from-B");
        Assert.DoesNotContain(clientB.Messages, m => m.Payload == "from-A");
    }

    [Fact]
    public async Task ReplaysMergedShadow_ToLateJoiningClient()
    {
        await using var harness = new ProxyTestHarness();
        var robot = await harness.AddRobotAsync("blidA", "passA");
        await harness.StartAsync();

        var shadowTopic = "$aws/things/blidA/shadow/update";

        // The robot emits two partial shadow updates before any client is listening.
        await robot.EmitAsync(shadowTopic, """{"state":{"reported":{"batPct":42}}}""");
        await robot.EmitAsync(shadowTopic, """{"state":{"reported":{"bin":{"full":true}}}}""");

        // Wait for the proxy to build the merged snapshot.
        await Task.Delay(500);

        // A late-joining client subscribes and must immediately get the full merged shadow (retained).
        var client = await harness.ConnectNativeAsync("blidA", "passA");
        await client.SubscribeAsync("$aws/things/blidA/shadow/#");

        await ProxyTestHarness.WaitUntilAsync(() => client.Has(shadowTopic), Timeout);

        var message = client.Find(shadowTopic)!.Value;
        using var doc = JsonDocument.Parse(message.Payload);
        var reported = doc.RootElement.GetProperty("state").GetProperty("reported");
        Assert.Equal(42, reported.GetProperty("batPct").GetInt32());
        Assert.True(reported.GetProperty("bin").GetProperty("full").GetBoolean());
    }

    [Fact]
    public async Task AggregatedPlainListener_MirrorsRobotsUnderNamespace()
    {
        await using var harness = new ProxyTestHarness();
        var robot = await harness.AddRobotAsync("blidA", "passA");
        await harness.StartAsync(enablePlain: true, aggregated: true);

        var watcher = await harness.ConnectPlainAsync("watcher");
        await watcher.SubscribeAsync("roomba/#");

        await robot.EmitAsync("wifistat", "signal");

        await ProxyTestHarness.WaitUntilAsync(() => watcher.Has("roomba/blidA/wifistat"), Timeout);
        Assert.Equal("signal", watcher.Find("roomba/blidA/wifistat")!.Value.Payload);
    }
}
