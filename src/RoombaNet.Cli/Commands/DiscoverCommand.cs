using System.CommandLine;
using System.Net;
using RoombaNet.Core;
using RoombaNet.Transport.Tls;

namespace RoombaNet.Cli.Commands;

public class DiscoverCommand : Command
{
    private readonly IRoombaDiscoveryService _roombaDiscoveryService;
    private readonly CancellationToken _cancellationToken;

    private readonly Option<string[]> _targetOption =
        new("--target")
        {
            Description = "Also query these IPs or CIDR ranges directly, e.g. --target 192.168.20.0/24. " +
                          "Needed when robots are on another subnet/VLAN, which broadcast cannot reach.",
            AllowMultipleArgumentsPerToken = true,
        };

    private readonly Option<int> _timeoutOption =
        new("--timeout") { Description = "Seconds to wait for replies", DefaultValueFactory = _ => 5 };

    public DiscoverCommand(
        IRoombaDiscoveryService roombaDiscoveryService,
        CancellationToken cancellationToken = default
    ) : base("discover", "Discover Roombas on the local network")
    {
        _roombaDiscoveryService = roombaDiscoveryService;
        _cancellationToken = cancellationToken;

        Options.Add(_targetOption);
        Options.Add(_timeoutOption);

        SetAction(async parseResult =>
        {
            List<IPAddress> targets;
            try
            {
                targets = DiscoveryTargets.Parse(parseResult.GetValue(_targetOption) ?? []);
            }
            catch (FormatException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            await DiscoverRoombas(parseResult.GetValue(_timeoutOption), targets);
            return 0;
        });
    }

    private async Task DiscoverRoombas(int timeoutSeconds, List<IPAddress> targets)
    {
        var roombas = await _roombaDiscoveryService.DiscoverRoombas(timeoutSeconds, targets, _cancellationToken);

        if (roombas.Count == 0)
        {
            Console.WriteLine(targets.Count == 0
                ? "No Roombas found on the network. If they are on another subnet/VLAN, use --target <ip|cidr>."
                : "No Roombas found on the network or the given targets.");
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

            if (roomba.Capabilities is { Count: > 0 })
            {
                var capabilities = string.Join(", ", roomba.Capabilities.Select(c => $"{c.Key}={c.Value}"));
                Console.WriteLine($"Capabilities:  {capabilities}");
            }

            Console.WriteLine();
        }
    }
}
