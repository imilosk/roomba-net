namespace RoombaNet.Api.Models;

/// <summary>
/// The robot's reported firmware and module versions, pulled together from the raw
/// shadow so a caller can see what the robot is running without digging through it.
/// All fields are read-only reports — RoombaNet does not push OTA updates.
/// </summary>
public record FirmwareInfo(
    string RobotId,
    string? SoftwareVersion,
    string? Sku,
    // Per-module versions (subModSwVer): navigation, mobility, power, safety, etc.
    string? Navigation,
    string? Mobility,
    string? Power,
    string? Safety,
    string? Linux,
    string? Connectivity,
    string? MobilityBootloader,
    // Charging base / Clean Base firmware, when a dock is known.
    string? DockFirmware,
    // Whether the robot reports a secure-boot block at all (sanmarino/m6 enforce it).
    bool SecureBootReported
)
{
    public static FirmwareInfo From(string robotId, RoombaState state)
    {
        var m = state.SubModSwVer;

        return new FirmwareInfo(
            RobotId: robotId,
            SoftwareVersion: state.SoftwareVer,
            Sku: state.Sku,
            Navigation: m?.Nav,
            Mobility: m?.Mob,
            Power: m?.Pwr,
            Safety: m?.Sft,
            Linux: m?.Linux,
            Connectivity: m?.Con,
            MobilityBootloader: m?.MobBtl,
            DockFirmware: state.Dock?.FwVer,
            SecureBootReported: state.SecureBoot is not null);
    }
}
