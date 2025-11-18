using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public partial class RoombaStatusService : BackgroundService
{
    private readonly IRoombaSubscriber _subscriber;
    private readonly ILogger<RoombaStatusService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<RoombaStatusUpdate> _statusChannel;
    private RoombaStatusUpdate _lastStatusUpdate = new(string.Empty, string.Empty, DateTime.MinValue);
    [GeneratedRegex(@"^\$aws/things/.+/shadow/update$")]
    private static partial Regex ShadowUpdateTopicRegex();

    public RoombaStatusService(
        IRoombaSubscriber subscriber,
        ILogger<RoombaStatusService> logger,
        TimeProvider timeProvider)
    {
        _subscriber = subscriber;
        _logger = logger;
        _timeProvider = timeProvider;

        // Unbounded channel to handle bursts of status updates
        _statusChannel = Channel.CreateUnbounded<RoombaStatusUpdate>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = true,
            });
    }

    public ChannelReader<RoombaStatusUpdate> StatusUpdates => _statusChannel.Reader;

    public RoombaStatusUpdate GetLastStatus()
    {
        return _lastStatusUpdate;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Roomba Status Service starting...");

        try
        {
            await _subscriber.Subscribe(messageEvent =>
            {
                var topic = messageEvent.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(messageEvent.ApplicationMessage.Payload);
                var timestamp = _timeProvider.GetUtcNow().DateTime;

                var statusUpdate = new RoombaStatusUpdate(topic, payload, timestamp);

                if (ShadowUpdateTopicRegex().IsMatch(topic))
                {
                    _lastStatusUpdate = statusUpdate;
                }

                _statusChannel.Writer.TryWrite(statusUpdate);

                _logger.LogDebug("Received status update on topic '{Topic}'", topic);
            }, stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Roomba Status Service is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Roomba Status Service encountered an error and will stop.");
        }
        finally
        {
            _statusChannel.Writer.Complete();
            _logger.LogInformation("Roomba Status Service stopped.");
        }
    }
}
