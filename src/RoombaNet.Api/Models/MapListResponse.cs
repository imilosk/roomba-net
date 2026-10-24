namespace RoombaNet.Api.Models;

public record MapListResponse(
    string RobotId,
    List<MapInfo> Maps
);

public record MapInfo(
    string PmapId,
    string UserPmapvId
);
