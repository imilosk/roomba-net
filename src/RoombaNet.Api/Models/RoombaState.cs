using System.Text.Json.Serialization;

namespace RoombaNet.Api.Models;

public class RoombaState
{
    [JsonPropertyName("batPct")]
    public int? BatPct { get; set; }

    [JsonPropertyName("batteryType")]
    public string? BatteryType { get; set; }

    [JsonPropertyName("batInfo")]
    public BatteryInfo? BatInfo { get; set; }

    [JsonPropertyName("batAuthEnable")]
    public bool? BatAuthEnable { get; set; }

    [JsonPropertyName("bbchg")]
    public BatteryCharge? Bbchg { get; set; }

    [JsonPropertyName("bbchg3")]
    public BatteryCharge3? Bbchg3 { get; set; }

    [JsonPropertyName("bbmssn")]
    public MissionStats? Bbmssn { get; set; }

    [JsonPropertyName("bbnav")]
    public NavigationStats? Bbnav { get; set; }

    [JsonPropertyName("bbpause")]
    public PauseStats? Bbpause { get; set; }

    [JsonPropertyName("bbrstinfo")]
    public ResetInfo? Bbrstinfo { get; set; }

    [JsonPropertyName("bbrun")]
    public RunStats? Bbrun { get; set; }

    [JsonPropertyName("bbswitch")]
    public SwitchStats? Bbswitch { get; set; }

    [JsonPropertyName("bbsys")]
    public SystemStats? Bbsys { get; set; }

    [JsonPropertyName("behaviorFwk")]
    public bool? BehaviorFwk { get; set; }

    [JsonPropertyName("bin")]
    public BinInfo? Bin { get; set; }

    [JsonPropertyName("binPause")]
    public bool? BinPause { get; set; }

    [JsonPropertyName("binTypeDetect")]
    public int? BinTypeDetect { get; set; }

    [JsonPropertyName("cap")]
    public Capabilities? Cap { get; set; }

    [JsonPropertyName("carpetBoost")]
    public bool? CarpetBoost { get; set; }

    [JsonPropertyName("childLock")]
    public bool? ChildLock { get; set; }

    [JsonPropertyName("cleanMissionStatus")]
    public CleanMissionStatus? CleanMissionStatus { get; set; }

    [JsonPropertyName("cleanSchedule2")]
    public List<object>? CleanSchedule2 { get; set; }

    [JsonPropertyName("cloudEnv")]
    public string? CloudEnv { get; set; }

    [JsonPropertyName("connected")]
    public bool? Connected { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("deploymentState")]
    public int? DeploymentState { get; set; }

    [JsonPropertyName("dock")]
    public DockInfo? Dock { get; set; }

    [JsonPropertyName("evacAllowed")]
    public bool? EvacAllowed { get; set; }

    [JsonPropertyName("ecoCharge")]
    public bool? EcoCharge { get; set; }

    [JsonPropertyName("featureFlags")]
    public FeatureFlags? FeatureFlags { get; set; }

    [JsonPropertyName("hwPartsRev")]
    public HardwarePartsRevision? HwPartsRev { get; set; }

    [JsonPropertyName("hwDbgr")]
    public object? HwDbgr { get; set; }

    [JsonPropertyName("langs2")]
    public LanguageInfo? Langs2 { get; set; }

    [JsonPropertyName("lastCommand")]
    public LastCommand? LastCommand { get; set; }

    [JsonPropertyName("lastDisconnect")]
    public int? LastDisconnect { get; set; }

    [JsonPropertyName("mapUploadAllowed")]
    public bool? MapUploadAllowed { get; set; }

    [JsonPropertyName("missionTelemetry")]
    public MissionTelemetry? MissionTelemetry { get; set; }

    [JsonPropertyName("mssnNavStats")]
    public MissionNavigationStats? MssnNavStats { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("noAutoPasses")]
    public bool? NoAutoPasses { get; set; }

    [JsonPropertyName("noPP")]
    public bool? NoPP { get; set; }

    [JsonPropertyName("openOnly")]
    public bool? OpenOnly { get; set; }

    [JsonPropertyName("pmapLearningAllowed")]
    public bool? PmapLearningAllowed { get; set; }

    [JsonPropertyName("pmaps")]
    public List<Dictionary<string, string>>? Pmaps { get; set; }

    [JsonPropertyName("pmapShare")]
    public PmapShare? PmapShare { get; set; }

    [JsonPropertyName("rankOverlap")]
    public int? RankOverlap { get; set; }

    [JsonPropertyName("reflexSettings")]
    public ReflexSettings? ReflexSettings { get; set; }

    [JsonPropertyName("sceneRecog")]
    public int? SceneRecog { get; set; }

    [JsonPropertyName("schedHold")]
    public bool? SchedHold { get; set; }

    [JsonPropertyName("secureBoot")]
    public SecureBoot? SecureBoot { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("softwareVer")]
    public string? SoftwareVer { get; set; }

    [JsonPropertyName("subModSwVer")]
    public SubModuleVersions? SubModSwVer { get; set; }

    [JsonPropertyName("svcEndpoints")]
    public ServiceEndpoints? SvcEndpoints { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("tls")]
    public TlsInfo? Tls { get; set; }

    [JsonPropertyName("twoPass")]
    public bool? TwoPass { get; set; }

    [JsonPropertyName("tz")]
    public TimezoneInfo? Tz { get; set; }

    [JsonPropertyName("vacHigh")]
    public bool? VacHigh { get; set; }

    [JsonPropertyName("netinfo")]
    public NetworkInfo? Netinfo { get; set; }

    [JsonPropertyName("signal")]
    public SignalInfo? Signal { get; set; }

    [JsonPropertyName("wifistat")]
    public WifiStatus? Wifistat { get; set; }

    [JsonPropertyName("wlcfg")]
    public WifiConfig? Wlcfg { get; set; }
}

public class BatteryInfo
{
    [JsonPropertyName("mDate")]
    public string? MDate { get; set; }

    [JsonPropertyName("mName")]
    public string? MName { get; set; }

    [JsonPropertyName("mDaySerial")]
    public int? MDaySerial { get; set; }

    [JsonPropertyName("mData")]
    public string? MData { get; set; }

    [JsonPropertyName("mLife")]
    public string? MLife { get; set; }

    [JsonPropertyName("cCount")]
    public int? CCount { get; set; }

    [JsonPropertyName("afCount")]
    public int? AfCount { get; set; }
}

public class BatteryCharge
{
    [JsonPropertyName("nChatters")]
    public int? NChatters { get; set; }

    [JsonPropertyName("nKnockoffs")]
    public int? NKnockoffs { get; set; }

    [JsonPropertyName("nLithF")]
    public int? NLithF { get; set; }

    [JsonPropertyName("nChgOk")]
    public int? NChgOk { get; set; }

    [JsonPropertyName("aborts")]
    public List<int>? Aborts { get; set; }

    [JsonPropertyName("chgErr")]
    public List<int>? ChgErr { get; set; }

    [JsonPropertyName("smberr")]
    public int? Smberr { get; set; }

    [JsonPropertyName("nChgErr")]
    public int? NChgErr { get; set; }
}

public class BatteryCharge3
{
    [JsonPropertyName("estCap")]
    public int? EstCap { get; set; }

    [JsonPropertyName("nAvail")]
    public int? NAvail { get; set; }

    [JsonPropertyName("hOnDock")]
    public int? HOnDock { get; set; }

    [JsonPropertyName("avgMin")]
    public int? AvgMin { get; set; }
}

public class MissionStats
{
    [JsonPropertyName("aCycleM")]
    public int? ACycleM { get; set; }

    [JsonPropertyName("nMssnF")]
    public int? NMssnF { get; set; }

    [JsonPropertyName("nMssnC")]
    public int? NMssnC { get; set; }

    [JsonPropertyName("nMssnOk")]
    public int? NMssnOk { get; set; }

    [JsonPropertyName("aMssnM")]
    public int? AMssnM { get; set; }

    [JsonPropertyName("nMssn")]
    public int? NMssn { get; set; }
}

public class NavigationStats
{
    [JsonPropertyName("aMtrack")]
    public int? AMtrack { get; set; }

    [JsonPropertyName("nGoodLmrks")]
    public int? NGoodLmrks { get; set; }

    [JsonPropertyName("aGain")]
    public int? AGain { get; set; }

    [JsonPropertyName("aExpo")]
    public int? AExpo { get; set; }
}

public class PauseStats
{
    [JsonPropertyName("pauses")]
    public List<int>? Pauses { get; set; }
}

public class ResetInfo
{
    [JsonPropertyName("nNavRst")]
    public int? NNavRst { get; set; }

    [JsonPropertyName("nMapLoadRst")]
    public int? NMapLoadRst { get; set; }

    [JsonPropertyName("nMobRst")]
    public int? NMobRst { get; set; }

    [JsonPropertyName("nSafRst")]
    public int? NSafRst { get; set; }

    [JsonPropertyName("safCauses")]
    public List<int>? SafCauses { get; set; }
}

public class RunStats
{
    [JsonPropertyName("nOvertemps")]
    public int? NOvertemps { get; set; }

    [JsonPropertyName("nCBump")]
    public int? NCBump { get; set; }

    [JsonPropertyName("nWStll")]
    public int? NWStll { get; set; }

    [JsonPropertyName("nMBStll")]
    public int? NMBStll { get; set; }

    [JsonPropertyName("nPanics")]
    public int? NPanics { get; set; }

    [JsonPropertyName("nEvacs")]
    public int? NEvacs { get; set; }

    [JsonPropertyName("nPicks")]
    public int? NPicks { get; set; }

    [JsonPropertyName("nOpticalDD")]
    public int? NOpticalDD { get; set; }

    [JsonPropertyName("nPiezoDD")]
    public int? NPiezoDD { get; set; }

    [JsonPropertyName("nStuck")]
    public int? NStuck { get; set; }

    [JsonPropertyName("sqft")]
    public int? Sqft { get; set; }

    [JsonPropertyName("nScrubs")]
    public int? NScrubs { get; set; }

    [JsonPropertyName("min")]
    public int? Min { get; set; }

    [JsonPropertyName("hr")]
    public int? Hr { get; set; }

    [JsonPropertyName("nCliffsF")]
    public int? NCliffsF { get; set; }

    [JsonPropertyName("nCliffsR")]
    public int? NCliffsR { get; set; }
}

public class SwitchStats
{
    [JsonPropertyName("nBumper")]
    public int? NBumper { get; set; }

    [JsonPropertyName("nDrops")]
    public int? NDrops { get; set; }

    [JsonPropertyName("nDock")]
    public int? NDock { get; set; }

    [JsonPropertyName("nSpot")]
    public int? NSpot { get; set; }

    [JsonPropertyName("nClean")]
    public int? NClean { get; set; }
}

public class SystemStats
{
    [JsonPropertyName("min")]
    public int? Min { get; set; }

    [JsonPropertyName("hr")]
    public int? Hr { get; set; }
}

public class BinInfo
{
    [JsonPropertyName("present")]
    public bool? Present { get; set; }

    [JsonPropertyName("full")]
    public bool? Full { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class Capabilities
{
    [JsonPropertyName("binFullDetect")]
    public int? BinFullDetect { get; set; }

    [JsonPropertyName("addOnHw")]
    public int? AddOnHw { get; set; }

    [JsonPropertyName("oMode")]
    public int? OMode { get; set; }

    [JsonPropertyName("dockComm")]
    public int? DockComm { get; set; }

    [JsonPropertyName("edge")]
    public int? Edge { get; set; }

    [JsonPropertyName("maps")]
    public int? Maps { get; set; }

    [JsonPropertyName("pmaps")]
    public int? Pmaps { get; set; }

    [JsonPropertyName("mc")]
    public int? Mc { get; set; }

    [JsonPropertyName("tLine")]
    public int? TLine { get; set; }

    [JsonPropertyName("area")]
    public int? Area { get; set; }

    [JsonPropertyName("eco")]
    public int? Eco { get; set; }

    [JsonPropertyName("multiPass")]
    public int? MultiPass { get; set; }

    [JsonPropertyName("team")]
    public int? Team { get; set; }

    [JsonPropertyName("pp")]
    public int? Pp { get; set; }

    [JsonPropertyName("lang")]
    public int? Lang { get; set; }

    [JsonPropertyName("5ghz")]
    public int? FiveGhz { get; set; }

    [JsonPropertyName("prov")]
    public int? Prov { get; set; }

    [JsonPropertyName("sched")]
    public int? Sched { get; set; }

    [JsonPropertyName("svcConf")]
    public int? SvcConf { get; set; }

    [JsonPropertyName("ota")]
    public int? Ota { get; set; }

    [JsonPropertyName("log")]
    public int? Log { get; set; }

    [JsonPropertyName("langOta")]
    public int? LangOta { get; set; }
}

public class CleanMissionStatus
{
    [JsonPropertyName("cycle")]
    public string? Cycle { get; set; }

    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    [JsonPropertyName("expireM")]
    public int? ExpireM { get; set; }

    [JsonPropertyName("rechrgM")]
    public int? RechrgM { get; set; }

    [JsonPropertyName("error")]
    public int? Error { get; set; }

    [JsonPropertyName("notReady")]
    public int? NotReady { get; set; }

    [JsonPropertyName("condNotReady")]
    public List<object>? CondNotReady { get; set; }

    [JsonPropertyName("mssnM")]
    public int? MssnM { get; set; }

    [JsonPropertyName("expireTm")]
    public int? ExpireTm { get; set; }

    [JsonPropertyName("rechrgTm")]
    public int? RechrgTm { get; set; }

    [JsonPropertyName("mssnStrtTm")]
    public long? MssnStrtTm { get; set; }

    [JsonPropertyName("operatingMode")]
    public int? OperatingMode { get; set; }

    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }

    [JsonPropertyName("nMssn")]
    public int? NMssn { get; set; }

    [JsonPropertyName("missionId")]
    public string? MissionId { get; set; }
}

public class DockInfo
{
    [JsonPropertyName("known")]
    public bool? Known { get; set; }

    [JsonPropertyName("pn")]
    public string? Pn { get; set; }

    [JsonPropertyName("state")]
    public int? State { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fwVer")]
    public string? FwVer { get; set; }

    [JsonPropertyName("hwRev")]
    public int? HwRev { get; set; }

    [JsonPropertyName("varID")]
    public int? VarID { get; set; }
}

public class FeatureFlags
{
    [JsonPropertyName("childLockEnable")]
    public int? ChildLockEnable { get; set; }

    [JsonPropertyName("hibLed")]
    public int? HibLed { get; set; }

    [JsonPropertyName("covPlan")]
    public int? CovPlan { get; set; }

    [JsonPropertyName("ros2SptLvl")]
    public bool? Ros2SptLvl { get; set; }

    [JsonPropertyName("clearHaz")]
    public bool? ClearHaz { get; set; }

    [JsonPropertyName("umcsIfNotDrc")]
    public int? UmcsIfNotDrc { get; set; }

    [JsonPropertyName("quietNav")]
    public bool? QuietNav { get; set; }
}

public class HardwarePartsRevision
{
    [JsonPropertyName("csscID")]
    public int? CsscID { get; set; }

    [JsonPropertyName("mobBrd")]
    public int? MobBrd { get; set; }

    [JsonPropertyName("mobBlid")]
    public string? MobBlid { get; set; }

    [JsonPropertyName("imuPartNo")]
    public string? ImuPartNo { get; set; }

    [JsonPropertyName("navSerialNo")]
    public string? NavSerialNo { get; set; }

    [JsonPropertyName("wlan0HwAddr")]
    public string? Wlan0HwAddr { get; set; }

    [JsonPropertyName("NavBrd")]
    public int? NavBrd { get; set; }
}

public class LanguageInfo
{
    [JsonPropertyName("sVer")]
    public string? SVer { get; set; }

    [JsonPropertyName("uLangs")]
    public object? ULangs { get; set; }

    [JsonPropertyName("dLangs")]
    public DownloadedLanguages? DLangs { get; set; }

    [JsonPropertyName("sLang")]
    public string? SLang { get; set; }

    [JsonPropertyName("aSlots")]
    public int? ASlots { get; set; }
}

public class DownloadedLanguages
{
    [JsonPropertyName("ver")]
    public string? Ver { get; set; }

    [JsonPropertyName("langs")]
    public List<string>? Langs { get; set; }
}

public class LastCommand
{
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }

    [JsonPropertyName("time")]
    public long? Time { get; set; }

    [JsonPropertyName("favorite_id")]
    public object? FavoriteId { get; set; }
}

public class MissionTelemetry
{
    [JsonPropertyName("aux_comms")]
    public int? AuxComms { get; set; }

    [JsonPropertyName("backup_counts")]
    public int? BackupCounts { get; set; }

    [JsonPropertyName("bat_stats")]
    public int? BatStats { get; set; }

    [JsonPropertyName("behaviors_report")]
    public int? BehaviorsReport { get; set; }

    [JsonPropertyName("camera_settings")]
    public int? CameraSettings { get; set; }

    [JsonPropertyName("coverage_report")]
    public int? CoverageReport { get; set; }

    [JsonPropertyName("map_hypotheses")]
    public int? MapHypotheses { get; set; }

    [JsonPropertyName("map_load")]
    public int? MapLoad { get; set; }

    [JsonPropertyName("map_save")]
    public int? MapSave { get; set; }

    [JsonPropertyName("mboe_landmark_stats")]
    public int? MboeLandmarkStats { get; set; }

    [JsonPropertyName("mboe_reloc")]
    public int? MboeReloc { get; set; }

    [JsonPropertyName("mission_stats")]
    public int? MissionStats { get; set; }

    [JsonPropertyName("pmap_navigability")]
    public int? PmapNavigability { get; set; }

    [JsonPropertyName("sensor_stats")]
    public int? SensorStats { get; set; }

    [JsonPropertyName("tumor_classifier_report")]
    public int? TumorClassifierReport { get; set; }

    [JsonPropertyName("vital_stats")]
    public int? VitalStats { get; set; }

    [JsonPropertyName("vslam_report")]
    public int? VslamReport { get; set; }
}

public class MissionNavigationStats
{
    [JsonPropertyName("nMssn")]
    public int? NMssn { get; set; }

    [JsonPropertyName("missionId")]
    public string? MissionId { get; set; }

    [JsonPropertyName("gLmk")]
    public int? GLmk { get; set; }

    [JsonPropertyName("lmk")]
    public int? Lmk { get; set; }

    [JsonPropertyName("reLc")]
    public int? ReLc { get; set; }

    [JsonPropertyName("plnErr")]
    public string? PlnErr { get; set; }

    [JsonPropertyName("mTrk")]
    public int? MTrk { get; set; }

    [JsonPropertyName("kdp")]
    public int? Kdp { get; set; }

    [JsonPropertyName("sfkdp")]
    public int? Sfkdp { get; set; }

    [JsonPropertyName("nmc")]
    public int? Nmc { get; set; }

    [JsonPropertyName("nmmc")]
    public int? Nmmc { get; set; }

    [JsonPropertyName("nrmc")]
    public int? Nrmc { get; set; }

    [JsonPropertyName("mpSt")]
    public string? MpSt { get; set; }

    [JsonPropertyName("l_drift")]
    public int? LDrift { get; set; }

    [JsonPropertyName("h_drift")]
    public int? HDrift { get; set; }

    [JsonPropertyName("l_squal")]
    public int? LSqual { get; set; }

    [JsonPropertyName("h_squal")]
    public int? HSqual { get; set; }
}

public class PmapShare
{
    [JsonPropertyName("copy")]
    public List<int>? Copy { get; set; }
}

public class ReflexSettings
{
    [JsonPropertyName("rlWheelDrop")]
    public WheelDropSettings? RlWheelDrop { get; set; }
}

public class WheelDropSettings
{
    [JsonPropertyName("enabled")]
    public int? Enabled { get; set; }
}

public class SecureBoot
{
    [JsonPropertyName("flip")]
    public int? Flip { get; set; }

    [JsonPropertyName("lastRst")]
    public string? LastRst { get; set; }

    [JsonPropertyName("recov")]
    public string? Recov { get; set; }

    [JsonPropertyName("idSwitch")]
    public int? IdSwitch { get; set; }

    [JsonPropertyName("TSK")]
    public string? TSK { get; set; }

    [JsonPropertyName("permReq")]
    public int? PermReq { get; set; }

    [JsonPropertyName("perm")]
    public string? Perm { get; set; }
}

public class SubModuleVersions
{
    [JsonPropertyName("nav")]
    public string? Nav { get; set; }

    [JsonPropertyName("mob")]
    public string? Mob { get; set; }

    [JsonPropertyName("pwr")]
    public string? Pwr { get; set; }

    [JsonPropertyName("sft")]
    public string? Sft { get; set; }

    [JsonPropertyName("mobBtl")]
    public string? MobBtl { get; set; }

    [JsonPropertyName("linux")]
    public string? Linux { get; set; }

    [JsonPropertyName("con")]
    public string? Con { get; set; }
}

public class ServiceEndpoints
{
    [JsonPropertyName("svcDeplId")]
    public string? SvcDeplId { get; set; }
}

public class TlsInfo
{
    [JsonPropertyName("tzbChk")]
    public int? TzbChk { get; set; }

    [JsonPropertyName("privKType")]
    public int? PrivKType { get; set; }

    [JsonPropertyName("lcCiphers")]
    public List<long>? LcCiphers { get; set; }
}

public class TimezoneInfo
{
    [JsonPropertyName("events")]
    public List<TimezoneEvent>? Events { get; set; }

    [JsonPropertyName("ver")]
    public int? Ver { get; set; }
}

public class TimezoneEvent
{
    [JsonPropertyName("dt")]
    public long? Dt { get; set; }

    [JsonPropertyName("off")]
    public int? Off { get; set; }
}

public class NetworkInfo
{
    [JsonPropertyName("dhcp")]
    public bool? Dhcp { get; set; }

    [JsonPropertyName("addr")]
    public string? Addr { get; set; }

    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    [JsonPropertyName("gw")]
    public string? Gw { get; set; }

    [JsonPropertyName("dns1")]
    public string? Dns1 { get; set; }

    [JsonPropertyName("dns2")]
    public string? Dns2 { get; set; }

    [JsonPropertyName("bssid")]
    public string? Bssid { get; set; }

    [JsonPropertyName("sec")]
    public int? Sec { get; set; }
}

public class SignalInfo
{
    [JsonPropertyName("rssi")]
    public int? Rssi { get; set; }

    [JsonPropertyName("snr")]
    public int? Snr { get; set; }

    [JsonPropertyName("noise")]
    public int? Noise { get; set; }
}

public class WifiStatus
{
    [JsonPropertyName("wifi")]
    public int? Wifi { get; set; }

    [JsonPropertyName("uap")]
    public bool? Uap { get; set; }

    [JsonPropertyName("cloud")]
    public int? Cloud { get; set; }
}

public class WifiConfig
{
    [JsonPropertyName("sec")]
    public int? Sec { get; set; }

    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }
}
