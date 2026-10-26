using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

[JsonSerializable(typeof(CommandPayload))]
[JsonSerializable(typeof(RegionCleanPayload))]
[JsonSerializable(typeof(ClearMapsPayload))]
[JsonSerializable(typeof(SettingPayload<bool>))]
[JsonSerializable(typeof(SettingPayload<int>))]
[JsonSerializable(typeof(SettingPayload<string>))]
[JsonSerializable(typeof(SettingPayload<PadWetnessSetting>))]
[JsonSerializable(typeof(SettingPayload<AudioSetting>))]
[JsonSerializable(typeof(SettingPayload<LanguageSetting>))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class RoombaJsonContext : JsonSerializerContext;
