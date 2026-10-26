using RoombaNet.Core.Constants;

namespace RoombaNet.Core;

public interface IRoombaSettingsService
{
    Task SetChildLock(bool enable, CancellationToken cancellationToken = default);
    Task SetBinPause(bool enable, CancellationToken cancellationToken = default);
    Task CleaningPasses(RoombaCleaningPasses passes, CancellationToken cancellationToken = default);
    Task SetRankOverlap(int value, CancellationToken cancellationToken = default);
    Task SetChargingLightPattern(int value, CancellationToken cancellationToken = default);
    Task SetPadWetness(int disposable, CancellationToken cancellationToken = default);

    /// <summary>Set vacuum suction power (Eco / Auto carpet-boost / Performance).</summary>
    Task SetCleaningPower(RoombaCleaningPower power, CancellationToken cancellationToken = default);

    /// <summary>Enable or disable edge cleaning. Maps to the inverse of <c>openOnly</c>.</summary>
    Task SetEdgeClean(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Rename the robot (as shown in apps and discovery).</summary>
    Task SetName(string name, CancellationToken cancellationToken = default);

    /// <summary>Battery-longevity charging mode (<c>ecoCharge</c>).</summary>
    Task SetEcoCharge(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Allow uploading the learned map to the cloud (<c>mapUploadAllowed</c>).</summary>
    Task SetMapUpload(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Allow persistent-map learning (<c>pmapLearningAllowed</c>).</summary>
    Task SetMapLearning(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Allow continuous persistent-map learning (<c>pmapCL</c>).</summary>
    Task SetContinuousMapLearning(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Bin-type auto-detection (<c>binTypeDetect</c>).</summary>
    Task SetBinTypeDetect(int value, CancellationToken cancellationToken = default);

    /// <summary>Enable or disable the perimeter/edge pass. Maps to the inverse of <c>noPP</c>.</summary>
    Task SetPerimeterPass(bool enable, CancellationToken cancellationToken = default);

    /// <summary>Set the IANA timezone (e.g. "Europe/Ljubljana").</summary>
    Task SetTimezone(string ianaTimezone, CancellationToken cancellationToken = default);

    /// <summary>Set speaker volume (nested <c>audio.volume</c>).</summary>
    Task SetVolume(int volume, CancellationToken cancellationToken = default);

    /// <summary>Select the robot voice language (nested <c>langs2.sLang</c>, e.g. "en-US_1").</summary>
    Task SetLanguage(string languageCode, CancellationToken cancellationToken = default);
}
