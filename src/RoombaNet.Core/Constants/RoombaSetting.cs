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

    // --- Additional settings mapped from the iRobot app (delta fields confirmed in device shadows) ---

    /// <summary>Battery-longevity charging mode.</summary>
    public const string EcoCharge = "ecoCharge";

    /// <summary>Allow the robot to upload its learned map to the cloud.</summary>
    public const string MapUploadAllowed = "mapUploadAllowed";

    /// <summary>Allow the robot to learn/build a persistent map.</summary>
    public const string PmapLearningAllowed = "pmapLearningAllowed";

    /// <summary>Allow continuous (ongoing) persistent-map learning.</summary>
    public const string PmapContinuousLearning = "pmapCL";

    /// <summary>Bin-type auto-detection (vacuum).</summary>
    public const string BinTypeDetect = "binTypeDetect";

    /// <summary>Perimeter/edge pass. Note the field is inverted: <c>noPP=true</c> means NO perimeter pass.</summary>
    public const string NoPerimeterPass = "noPP";

    /// <summary>IANA timezone string, e.g. "Europe/Ljubljana".</summary>
    public const string Timezone = "timezone";

    /// <summary>Speaker volume (nested <c>audio.volume</c>).</summary>
    public const string Audio = "audio";

    /// <summary>Voice language selection (nested <c>langs2.sLang</c>).</summary>
    public const string Languages = "langs2";
}
