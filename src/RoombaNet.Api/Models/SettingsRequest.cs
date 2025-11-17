namespace RoombaNet.Api.Models;

public record EnableSettingRequest(
    bool Enable
);

public record CleaningPassesRequest(
    int Passes
);
