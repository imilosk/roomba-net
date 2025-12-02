using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class DiscoverCommand : Command
{
    private readonly IRoombaSettingsClient _roombaSettingsClient;
    private readonly CancellationToken _cancellationToken;

    public DiscoverCommand(
        IRoombaSettingsClient roombaSettingsClient,
        CancellationToken cancellationToken = default
    ) : base("discover", "Discover Roombas on the local network")
    {
        _roombaSettingsClient = roombaSettingsClient;
        _cancellationToken = cancellationToken;

        SetAction(async _ => await DiscoverRoombas());
    }

    private async Task DiscoverRoombas()
    {
        var roombas = await _roombaSettingsClient.GetRoombas(_cancellationToken);

        if (roombas.Count == 0)
        {
            Console.WriteLine("No Roombas found on the network.");
            return;
        }

        Console.WriteLine($"Found {roombas.Count} Roomba(s):");
        Console.WriteLine();

        foreach (var roomba in roombas)
        {
            var name = string.IsNullOrEmpty(roomba.RobotName) ? "Unnamed" : roomba.RobotName;
            Console.WriteLine($"Name:          {name}");
            Console.WriteLine($"BLID:          {roomba.Blid}");
            Console.WriteLine($"IP Address:    {roomba.Ip}");
            Console.WriteLine($"Hostname:      {roomba.Hostname}");
            Console.WriteLine($"MAC Address:   {roomba.Mac}");
            Console.WriteLine($"SKU/Model:     {roomba.Sku}");
            Console.WriteLine();
        }
    }
}
