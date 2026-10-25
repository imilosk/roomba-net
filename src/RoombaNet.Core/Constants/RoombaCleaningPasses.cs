namespace RoombaNet.Core.Constants;

public enum RoombaCleaningPasses
{
    OnePass = 1,
    TwoPass = 2,
    RoomSizeClean = 3,
}

public enum ChargingLightPattern
{
    DockingAndCharging = 0,
    DockingOnly = 1,
    NoLights = 2,
}

public enum PadWetnessLevel
{
    Eco = 1,
    Standard = 2,
    Ultra = 3,
}

/// <summary>
/// Vacuum suction power mode. Maps to the <c>carpetBoost</c> and <c>vacHigh</c> shadow flags.
/// </summary>
public enum RoombaCleaningPower
{
    /// <summary>Lowest power / quietest. carpetBoost=false, vacHigh=false.</summary>
    Eco = 0,

    /// <summary>Automatically boost on carpet. carpetBoost=true, vacHigh=false.</summary>
    Auto = 1,

    /// <summary>Maximum suction everywhere. carpetBoost=false, vacHigh=true.</summary>
    Performance = 2,
}
