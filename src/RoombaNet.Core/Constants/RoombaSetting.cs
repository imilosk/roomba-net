namespace RoombaNet.Core.Constants;

public static class RoombaSetting
{
    public const string ChildLock = "childLock";
    public const string BinPause = "binPause";
    public const string TwoPass = "twoPass";
    public const string NoAutoPasses = "noAutoPasses";
    public const string RankOverlap = "rankOverlap";
    public const string PadWetness = "padWetness";
    public const string ChargingLightPattern = "chrgLrPtrn";
    public const string CarpetBoost = "carpetBoost";
    public const string VacHigh = "vacHigh";
    public const string OpenOnly = "openOnly";
    public const string Name = "name";

    // Persistent maps live in the device shadow; publishing an empty array clears them.
    public const string Pmaps = "pmaps";
}
