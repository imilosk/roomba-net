using RoombaNet.Proxy;
using Xunit;

namespace RoombaNet.Proxy.Tests;

public class ProxyTopicsTests
{
    [Theory]
    [InlineData("cmd", true)]
    [InlineData("delta", true)]
    [InlineData("wifictl", true)]
    [InlineData("local/rrtp/request", true)]
    [InlineData("cloud/rrtp/request", true)]
    [InlineData("wifistat", false)]
    [InlineData("foo", false)]
    [InlineData("", false)]
    public void IsRobotBound_MatchesStaticAllowlist(string topic, bool expected)
    {
        Assert.Equal(expected, ProxyTopics.IsRobotBound(topic, "blid123"));
    }

    [Fact]
    public void IsRobotBound_AllowsOwnShadowGetOnly()
    {
        Assert.True(ProxyTopics.IsRobotBound("$aws/things/blid123/shadow/get", "blid123"));
        Assert.False(ProxyTopics.IsRobotBound("$aws/things/other/shadow/get", "blid123"));
    }

    [Fact]
    public void UpstreamSender_RoundTrips()
    {
        var marker = ProxyTopics.UpstreamSender("blidABC");
        Assert.Equal("blidABC", ProxyTopics.TryGetUpstreamBlid(marker));
        Assert.Null(ProxyTopics.TryGetUpstreamBlid("blidABC"));
        Assert.Null(ProxyTopics.TryGetUpstreamBlid(null));
    }
}
