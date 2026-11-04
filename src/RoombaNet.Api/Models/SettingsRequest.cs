namespace RoombaNet.Api.Models;

public record EnableSettingRequest(
    bool Enable
);

public record CleaningPassesRequest(
    int Passes
);

public record IntSettingRequest(
    int Value
);

public record StringSettingRequest(
    string Value
);
