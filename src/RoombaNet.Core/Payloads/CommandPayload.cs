using System.Text.Json.Serialization;

namespace RoombaNet.Core.Payloads;

public readonly struct CommandPayload
{
    public CommandPayload(string command, long time, string initiator)
    {
        Command = command;
        Time = time;
        Initiator = initiator;
    }

    [JsonPropertyName("command")]
    public string Command { get; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; }

    [JsonPropertyName("initiator")]
    public string Initiator { get; } = string.Empty;
}