using System.Text.Json.Serialization;

namespace RoombaNet.Mqtt.Payloads;

[JsonSerializable(typeof(CommandPayload))]
[JsonSerializable(typeof(SettingPayload<bool>))]
[JsonSerializable(typeof(SettingPayload<int>))]
[JsonSerializable(typeof(SettingPayload<string>))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class RoombaJsonContext : JsonSerializerContext;