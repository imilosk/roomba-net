using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

/// <summary>Nested <c>langs2</c> delta object. Sent as <c>{"langs2":{"sLang":"en-US_1"}}</c>.</summary>
public readonly struct LanguageSetting
{
    [JsonPropertyName("sLang")]
    public string SelectedLanguage { get; init; }
}
