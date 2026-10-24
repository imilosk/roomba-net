namespace RoombaNet.Api.Models;

public record CleanRegionsRequest(
    string PmapId,
    List<RegionRequest> Regions,
    string? UserPmapvId = null,
    bool? Ordered = true
);

public record RegionRequest(
    string RegionId,
    string? Type = "rid"
);

public record CleaningPowerRequest(
    int Power
);

public record SetNameRequest(
    string Name
);
