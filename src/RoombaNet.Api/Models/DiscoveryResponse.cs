namespace RoombaNet.Api.Models;

public record DiscoveryResponse
{
    public bool Success { get; init; }
    public List<DiscoveredRoomba> Roombas { get; init; } = [];
    public string? Error { get; init; }
}

public record DiscoveredRoomba
{
    public required string Blid { get; init; }
    public required string RobotName { get; init; }
    public required string Ip { get; init; }
    public required string Mac { get; init; }
    public required string Hostname { get; init; }
    public string? SoftwareVersion { get; init; }
    public string? Sku { get; init; }
}
