using System.CommandLine;
using RoombaNet.Core;

namespace RoombaNet.Cli.Commands;

public class DiscoverCommand : Command
{
    private readonly IRoombaDiscoveryService _roombaDiscoveryService;
    private readonly CancellationToken _cancellationToken;

    public DiscoverCommand(
        IRoombaDiscoveryService roombaDiscoveryService,
        CancellationToken cancellationToken = default
    ) : base("discover", "Discover Roombas on the local network")
    {
        _roombaDiscoveryService = roombaDiscoveryService;
        _cancellationToken = cancellationToken;

        SetAction(async _ => await DiscoverRoombas());
    }

    private async Task DiscoverRoombas()
    {
        var roombas = await _roombaDiscoveryService.DiscoverRoombas(cancellationToken: _cancellationToken);

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
            Console.WriteLine($"Software:      {roomba.SoftwareVersion}");
            Console.WriteLine($"Protocol:      {roomba.Protocol}");

            // if (roomba.Nc.HasValue)
            // {
            //     Console.WriteLine($"NC:            {roomba.Nc}");
            // }

            // if (roomba.Capabilities is not null && roomba.Capabilities.Count > 0)
            // {
            //     Console.WriteLine("Capabilities:");
            //     foreach (var cap in roomba.Capabilities)
            //     {
            //         Console.WriteLine($"  {cap.Key}: {cap.Value}");
            //     }
            // }

            Console.WriteLine();
        }
    }
}
