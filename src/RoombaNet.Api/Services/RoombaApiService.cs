using System.Net;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services.RoombaClients;
using RoombaNet.Api.Services.RobotRegistry;
using MQTTnet;
using RoombaNet.Core;
using RoombaNet.Core.Constants;
using RoombaNet.Core.Payloads;
using RoombaNet.Core.WifiConfig;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Api.Services;

public class RoombaApiService
{
    private readonly IRoombaDiscoveryService _discoveryService;
    private readonly IRoombaClientFactory _clientFactory;
    private readonly IRobotRegistry _registry;
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RoombaApiService> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly string[] AvailableCommands =
    [
        "find", "start", "stop", "pause", "resume", "dock", "evac", "reset", "train",
        "spot", "wake", "sleep", "off", "tidy", "skip", "stopevac", "wlapon", "wlapoff",
    ];

    public RoombaApiService(
        IRoombaClientFactory clientFactory,
        IRoombaDiscoveryService discoveryService,
        IRobotRegistry registry,
        MqttClientFactory mqttClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<RoombaApiService> logger,
        TimeProvider timeProvider)
    {
        _clientFactory = clientFactory;
        _discoveryService = discoveryService;
        _registry = registry;
        _mqttClientFactory = mqttClientFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResponse> ExecuteCommandAsync(
        string command,
        string robotId,
        CancellationToken cancellationToken = default
    )
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Executing command '{Command}' with execution ID '{ExecutionId}'",
                command,
                executionId
            );

            if (!IsValidCommand(command))
            {
                var invalidMessage = $"Invalid command: {command}";
                _logger.LogWarning(invalidMessage);

                return new CommandResponse(
                    Success: false,
                    Command: command,
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidCommand",
                    Details: $"Available commands: {string.Join(", ", AvailableCommands)}"
                );
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await ExecuteRoombaCommand(client.Commands, command, cancellationToken);

            var successMessage = $"Command '{command}' executed successfully";
            _logger.LogInformation(successMessage);

            return new CommandResponse(
                Success: true,
                Command: command,
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to execute command '{command}'";
            _logger.LogError(ex, errorMessage);

            return new CommandResponse(
                Success: false,
                Command: command,
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<BatchCommandResponse> ExecuteBatchCommandsAsync(
        BatchCommandRequest request,
        string robotId,
        CancellationToken cancellationToken = default
    )
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;
        var results = new List<BatchCommandResult>();

        _logger.LogInformation(
            "Executing batch commands with execution ID '{ExecutionId}'. Sequential: {Sequential}",
            executionId,
            request.Sequential);

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);

            if (request.Sequential)
            {
                foreach (var command in request.Commands)
                {
                    var result = await ExecuteSingleBatchCommand(client.Commands, command, cancellationToken);
                    results.Add(result);
                }
            }
            else
            {
                var tasks = request.Commands
                    .Select(command => ExecuteSingleBatchCommand(client.Commands, command, cancellationToken));
                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);
            }
        }
        catch (Exception ex)
        {
            results.Add(new BatchCommandResult(
                Command: "batch",
                Success: false,
                Message: "Failed to execute batch commands",
                Error: ex.Message));
        }

        var overallSuccess = results.All(r => r.Success);

        return new BatchCommandResponse(
            Success: overallSuccess,
            Results: results.ToArray(),
            Timestamp: timestamp,
            ExecutionId: executionId
        );
    }

    public Task<AvailableCommandsResponse> GetAvailableCommandsAsync()
    {
        var response = new AvailableCommandsResponse(
            Commands: AvailableCommands,
            Timestamp: _timeProvider.GetUtcNow().DateTime
        );

        return Task.FromResult(response);
    }

    public async Task<HealthCheckResponse> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Real check: the registry (SQLite) must be reachable and queryable.
            var robots = await _registry.GetAll(cancellationToken);

            return new HealthCheckResponse(
                IsHealthy: true,
                Status: $"Healthy - registry reachable, {robots.Count} robot(s) registered",
                Timestamp: _timeProvider.GetUtcNow().DateTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            return new HealthCheckResponse(
                IsHealthy: false,
                Status: "Unhealthy - robot registry is not reachable",
                Timestamp: _timeProvider.GetUtcNow().DateTime,
                Error: ex.Message
            );
        }
    }

    public async Task<CommandResponse> CancelAsync(
        string robotId,
        bool returnToDock,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);

            // `stop` ends the mission (cycle -> none), which also cancels any pending
            // recharge-and-resume so the robot will NOT restart after charging.
            await client.Commands.Stop(cancellationToken);

            if (returnToDock)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                await client.Commands.Dock(cancellationToken);
            }

            var message = returnToDock
                ? "Mission cancelled; returning to dock. It will not auto-resume."
                : "Mission cancelled. It will not auto-resume after charging.";

            return new CommandResponse(true, "cancel", message, timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel mission");
            return new CommandResponse(false, "cancel", "Failed to cancel mission", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    private async Task<BatchCommandResult> ExecuteSingleBatchCommand(
        IRoombaCommandService commandService,
        string command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!IsValidCommand(command))
            {
                return new BatchCommandResult(
                    Command: command,
                    Success: false,
                    Message: $"Invalid command: {command}",
                    Error: "InvalidCommand"
                );
            }

            await ExecuteRoombaCommand(commandService, command, cancellationToken);

            return new BatchCommandResult(
                Command: command,
                Success: true,
                Message: $"Command '{command}' executed successfully"
            );
        }
        catch (Exception ex)
        {
            return new BatchCommandResult(
                Command: command,
                Success: false,
                Message: $"Failed to execute command '{command}'",
                Error: ex.Message
            );
        }
    }

    private static async Task ExecuteRoombaCommand(
        IRoombaCommandService commandService,
        string command,
        CancellationToken cancellationToken)
    {
        // All simple commands publish the same {command,time,initiator} shape to the `cmd`
        // topic, so route them through the generic Send rather than a per-command switch.
        await commandService.Send(command.ToLowerInvariant(), cancellationToken);
    }

    private static bool IsValidCommand(string command)
    {
        return AvailableCommands.Contains(command.ToLowerInvariant());
    }

    public async Task<SettingsResponse> SetChildLockAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting child lock to '{Enable}' with execution ID '{ExecutionId}'",
                enable,
                executionId
            );

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetChildLock(enable, cancellationToken);

            var successMessage = $"Child lock {(enable ? "enabled" : "disabled")} successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "ChildLock",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to set child lock to '{enable}'";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "ChildLock",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<SettingsResponse> SetBinPauseAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting bin pause to '{Enable}' with execution ID '{ExecutionId}'",
                enable,
                executionId
            );

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetBinPause(enable, cancellationToken);

            var successMessage = $"Bin pause {(enable ? "enabled" : "disabled")} successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "BinPause",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to set bin pause to '{enable}'";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "BinPause",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<SettingsResponse> SetCleaningPassesAsync(
        int passes,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting cleaning passes to '{Passes}' with execution ID '{ExecutionId}'",
                passes,
                executionId
            );

            if (!Enum.IsDefined(typeof(RoombaCleaningPasses), passes))
            {
                var invalidMessage =
                    $"Invalid cleaning passes value: {passes}. Valid values are 1 (OnePass), 2 (TwoPass), or 3 (RoomSizeClean)";
                _logger.LogWarning(invalidMessage);

                return new SettingsResponse(
                    Success: false,
                    Setting: "CleaningPasses",
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidValue",
                    Details: "Valid values: 1, 2, or 3"
                );
            }

            var cleaningPasses = (RoombaCleaningPasses)passes;
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.CleaningPasses(cleaningPasses, cancellationToken);

            var successMessage = $"Cleaning passes set to {cleaningPasses} successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "CleaningPasses",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to set cleaning passes to '{passes}'";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "CleaningPasses",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<SettingsResponse> SetRankOverlapAsync(
        int value,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting rank overlap to '{Value}' with execution ID '{ExecutionId}'",
                value,
                executionId
            );

            if (value is < 0 or > 100)
            {
                var invalidMessage =
                    $"Invalid rank overlap value: {value}. Valid values are 0-100.";
                _logger.LogWarning(invalidMessage);

                return new SettingsResponse(
                    Success: false,
                    Setting: "RankOverlap",
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidValue",
                    Details: "Valid values: 0-100"
                );
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetRankOverlap(value, cancellationToken);

            var successMessage = $"Rank overlap set to {value} successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "RankOverlap",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to set rank overlap to '{value}'";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "RankOverlap",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<SettingsResponse> SetLiquidAmountAsync(
        int value,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting liquid amount to {Value} with execution ID '{ExecutionId}'",
                value,
                executionId
            );

            if (!Enum.IsDefined(typeof(PadWetnessLevel), value))
            {
                var invalidMessage =
                    $"Invalid liquid amount value. Valid values are 1 (Eco), 2 (Standard), or 3 (Ultra)";
                _logger.LogWarning(invalidMessage);

                return new SettingsResponse(
                    Success: false,
                    Setting: "LiquidAmount",
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidValue",
                    Details: "Valid values: 1, 2, or 3"
                );
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetPadWetness(value, cancellationToken);

            var successMessage = "Liquid amount set successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "LiquidAmount",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = "Failed to set liquid amount";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "LiquidAmount",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<SettingsResponse> SetChargingLightPatternAsync(
        int value,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            _logger.LogInformation(
                "Setting charging light pattern to '{Value}' with execution ID '{ExecutionId}'",
                value,
                executionId
            );

            if (!Enum.IsDefined(typeof(ChargingLightPattern), value))
            {
                var invalidMessage =
                    $"Invalid charging light pattern value: {value}. Valid values are 0 (DockingAndCharging), 1 (DockingOnly), or 2 (NoLights)";
                _logger.LogWarning(invalidMessage);

                return new SettingsResponse(
                    Success: false,
                    Setting: "ChargingLightPattern",
                    Message: invalidMessage,
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "InvalidValue",
                    Details: "Valid values: 0, 1, or 2"
                );
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetChargingLightPattern(value, cancellationToken);

            var successMessage = $"Charging light pattern set to {value} successfully";
            _logger.LogInformation(successMessage);

            return new SettingsResponse(
                Success: true,
                Setting: "ChargingLightPattern",
                Message: successMessage,
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to set charging light pattern to '{value}'";
            _logger.LogError(ex, errorMessage);

            return new SettingsResponse(
                Success: false,
                Setting: "ChargingLightPattern",
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<DiscoveryResponse> DiscoverRoombas(
        int timeoutSeconds = 5,
        IReadOnlyCollection<IPAddress>? targets = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation("Starting Roomba discovery with timeout: {Timeout}s", timeoutSeconds);

            var roombas = await _discoveryService.DiscoverRoombas(timeoutSeconds, targets, cancellationToken);

            var discoveredRoombas = roombas.Select(r => new DiscoveredRoomba
                {
                    Blid = r.Blid,
                    RobotName = r.RobotName,
                    Ip = r.Ip,
                    Mac = r.Mac,
                    Hostname = r.Hostname,
                    SoftwareVersion = r.SoftwareVersion,
                    Sku = r.Sku
                })
                .ToList();

            _logger.LogInformation("Discovery completed. Found {Count} Roomba(s)", discoveredRoombas.Count);

            return new DiscoveryResponse
            {
                Success = true,
                Roombas = discoveredRoombas
            };
        }
        catch (Exception ex)
        {
            var errorMessage = "Failed to discover Roombas";
            _logger.LogError(ex, errorMessage);

            return new DiscoveryResponse
            {
                Success = false,
                Error = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    public async Task<WifiConfigureResponse> ConfigureWifiAsync(
        string robotId,
        string ssid,
        string password,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        if (string.IsNullOrWhiteSpace(ssid) || string.IsNullOrWhiteSpace(password))
        {
            return new WifiConfigureResponse(
                Success: false,
                Message: "SSID and password are required.",
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: "InvalidRequest"
            );
        }

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            var settings = new RoombaSettings
            {
                Blid = client.Settings.Blid,
                Ip = client.Settings.Ip,
                Port = client.Settings.Port,
                Password = client.Settings.Password
            };

            var publisher = new MqttPublisher(_loggerFactory.CreateLogger<MqttPublisher>());
            var commandBuilder = new WifiConfigCommandBuilder(
                _loggerFactory.CreateLogger<WifiConfigCommandBuilder>(),
                publisher);

            var wifiService = new RoombaWifiService(
                _loggerFactory.CreateLogger<RoombaWifiService>(),
                _mqttClientFactory,
                settings,
                commandBuilder);

            var request = new WifiConfigurationRequest
            {
                Ssid = ssid,
                Password = password
            };

            var success = await wifiService.ConfigureWifiAsync(request, cancellationToken);
            if (!success)
            {
                return new WifiConfigureResponse(
                    Success: false,
                    Message: "Failed to configure Wi-Fi.",
                    Timestamp: timestamp,
                    ExecutionId: executionId,
                    Error: "WifiConfigFailed"
                );
            }

            return new WifiConfigureResponse(
                Success: true,
                Message: "Wi-Fi configuration sent successfully.",
                Timestamp: timestamp,
                ExecutionId: executionId
            );
        }
        catch (Exception ex)
        {
            var errorMessage = "Failed to configure Wi-Fi.";
            _logger.LogError(ex, errorMessage);

            return new WifiConfigureResponse(
                Success: false,
                Message: errorMessage,
                Timestamp: timestamp,
                ExecutionId: executionId,
                Error: ex.GetType().Name,
                Details: ex.Message
            );
        }
    }

    public async Task<CommandResponse> SendRawCommandAsync(
        string command,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        if (string.IsNullOrWhiteSpace(command))
        {
            return new CommandResponse(false, command, "Command must not be empty", timestamp, executionId,
                "InvalidCommand");
        }

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.Commands.Send(command, cancellationToken);

            return new CommandResponse(true, command, $"Command '{command}' sent successfully", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send raw command '{Command}'", command);
            return new CommandResponse(false, command, $"Failed to send command '{command}'", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<CommandResponse> CleanRegionsAsync(
        CleanRegionsRequest request,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (request.Regions is null || request.Regions.Count == 0)
            {
                return new CommandResponse(false, "clean-regions", "At least one region is required", timestamp,
                    executionId, "InvalidRequest");
            }

            var regions = request.Regions
                .Select(r => new Region { RegionId = r.RegionId, Type = string.IsNullOrWhiteSpace(r.Type) ? "rid" : r.Type })
                .ToList();

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.Commands.CleanRegions(
                request.PmapId,
                regions,
                request.UserPmapvId,
                request.Ordered ?? true,
                cancellationToken);

            return new CommandResponse(true, "clean-regions",
                $"Region clean started for {regions.Count} region(s)", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start region clean");
            return new CommandResponse(false, "clean-regions", "Failed to start region clean", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<CommandResponse> ClearMapsAsync(
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            var success = await client.Maps.ClearAllMaps(cancellationToken);

            return success
                ? new CommandResponse(true, "clear-maps", "All persistent maps cleared", timestamp, executionId)
                : new CommandResponse(false, "clear-maps", "Failed to clear maps", timestamp, executionId,
                    "PublishFailed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear maps");
            return new CommandResponse(false, "clear-maps", "Failed to clear maps", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetCleaningPowerAsync(
        int power,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (!Enum.IsDefined(typeof(RoombaCleaningPower), power))
            {
                return new SettingsResponse(false, "CleaningPower",
                    $"Invalid cleaning power: {power}. Valid: 0 (Eco), 1 (Auto), 2 (Performance)",
                    timestamp, executionId, "InvalidValue", "Valid values: 0, 1, 2");
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetCleaningPower((RoombaCleaningPower)power, cancellationToken);

            return new SettingsResponse(true, "CleaningPower",
                $"Cleaning power set to {(RoombaCleaningPower)power}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cleaning power");
            return new SettingsResponse(false, "CleaningPower", "Failed to set cleaning power", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetEdgeCleanAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetEdgeClean(enable, cancellationToken);

            return new SettingsResponse(true, "EdgeClean",
                $"Edge cleaning {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set edge clean");
            return new SettingsResponse(false, "EdgeClean", "Failed to set edge clean", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetNameAsync(
        string name,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new SettingsResponse(false, "Name", "Name must not be empty", timestamp, executionId,
                    "InvalidValue");
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetName(name, cancellationToken);

            return new SettingsResponse(true, "Name", $"Robot renamed to '{name}'", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set name");
            return new SettingsResponse(false, "Name", "Failed to set name", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetEcoChargeAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetEcoCharge(enable, cancellationToken);

            return new SettingsResponse(true, "EcoCharge",
                $"Eco charge {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set eco charge");
            return new SettingsResponse(false, "EcoCharge", "Failed to set eco charge", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetMapUploadAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetMapUpload(enable, cancellationToken);

            return new SettingsResponse(true, "MapUpload",
                $"Map upload {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set map upload");
            return new SettingsResponse(false, "MapUpload", "Failed to set map upload", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetMapLearningAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetMapLearning(enable, cancellationToken);

            return new SettingsResponse(true, "MapLearning",
                $"Map learning {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set map learning");
            return new SettingsResponse(false, "MapLearning", "Failed to set map learning", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetContinuousMapLearningAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetContinuousMapLearning(enable, cancellationToken);

            return new SettingsResponse(true, "ContinuousMapLearning",
                $"Continuous map learning {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set continuous map learning");
            return new SettingsResponse(false, "ContinuousMapLearning", "Failed to set continuous map learning",
                timestamp, executionId, ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetPerimeterPassAsync(
        bool enable,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetPerimeterPass(enable, cancellationToken);

            return new SettingsResponse(true, "PerimeterPass",
                $"Perimeter pass {(enable ? "enabled" : "disabled")}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set perimeter pass");
            return new SettingsResponse(false, "PerimeterPass", "Failed to set perimeter pass", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetBinTypeDetectAsync(
        int value,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetBinTypeDetect(value, cancellationToken);

            return new SettingsResponse(true, "BinTypeDetect",
                $"Bin-type detection set to {value}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bin-type detection");
            return new SettingsResponse(false, "BinTypeDetect", "Failed to set bin-type detection", timestamp,
                executionId, ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetVolumeAsync(
        int volume,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (volume is < 0 or > 100)
            {
                return new SettingsResponse(false, "Volume",
                    $"Invalid volume: {volume}. Valid values are 0-100.", timestamp, executionId,
                    "InvalidValue", "Valid values: 0-100");
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetVolume(volume, cancellationToken);

            return new SettingsResponse(true, "Volume", $"Volume set to {volume}", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set volume");
            return new SettingsResponse(false, "Volume", "Failed to set volume", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetTimezoneAsync(
        string ianaTimezone,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (string.IsNullOrWhiteSpace(ianaTimezone))
            {
                return new SettingsResponse(false, "Timezone", "Timezone must not be empty", timestamp, executionId,
                    "InvalidValue");
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetTimezone(ianaTimezone, cancellationToken);

            return new SettingsResponse(true, "Timezone", $"Timezone set to '{ianaTimezone}'", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set timezone");
            return new SettingsResponse(false, "Timezone", "Failed to set timezone", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }

    public async Task<SettingsResponse> SetLanguageAsync(
        string languageCode,
        string robotId,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var timestamp = _timeProvider.GetUtcNow().DateTime;

        try
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return new SettingsResponse(false, "Language", "Language code must not be empty", timestamp,
                    executionId, "InvalidValue");
            }

            var client = await _clientFactory.GetClient(robotId, cancellationToken);
            await client.SettingsService.SetLanguage(languageCode, cancellationToken);

            return new SettingsResponse(true, "Language", $"Language set to '{languageCode}'", timestamp, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set language");
            return new SettingsResponse(false, "Language", "Failed to set language", timestamp, executionId,
                ex.GetType().Name, ex.Message);
        }
    }
}
