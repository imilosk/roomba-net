using System.CommandLine;
using RoombaNet.Core;
using RoombaNet.Core.Payloads;

namespace RoombaNet.Cli.Commands;

public class CleanRegionsCommand : Command
{
    private readonly IRoombaCommandService _commandService;
    private readonly CancellationToken _cancellationToken;

    private readonly Option<string> _pmapOption =
        new("--pmap") { Description = "The persistent map id (pmap_id) to clean", Required = true };

    private readonly Option<string[]> _regionsOption =
        new("--regions") { Description = "Region ids to clean, e.g. --regions 1 3 5", Required = true, AllowMultipleArgumentsPerToken = true };

    private readonly Option<string?> _versionOption =
        new("--version") { Description = "Optional user_pmapv_id (map version)" };

    private readonly Option<bool> _unorderedOption =
        new("--unordered") { Description = "Let the robot optimize region order instead of cleaning in the given order" };

    public CleanRegionsCommand(
        IRoombaCommandService commandService,
        CancellationToken cancellationToken = default
    ) : base("clean-regions", "Clean specific rooms/zones within a persistent map")
    {
        _commandService = commandService;
        _cancellationToken = cancellationToken;

        Aliases.Add("clean-rooms");
        Options.Add(_pmapOption);
        Options.Add(_regionsOption);
        Options.Add(_versionOption);
        Options.Add(_unorderedOption);

        SetAction(async parseResult =>
        {
            var pmap = parseResult.GetValue(_pmapOption)!;
            var regionIds = parseResult.GetValue(_regionsOption) ?? [];
            var version = parseResult.GetValue(_versionOption);
            var ordered = !parseResult.GetValue(_unorderedOption);

            var regions = regionIds
                .Select(id => new Region { RegionId = id, Type = "rid" })
                .ToList();

            await _commandService.CleanRegions(pmap, regions, version, ordered, _cancellationToken);
            Console.WriteLine($"✅ Region clean started for {regions.Count} region(s) on map {pmap}.");
        });
    }
}
