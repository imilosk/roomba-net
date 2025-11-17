using System.Text;
using System.Threading.Channels;
using RoombaNet.Api.Models;
using RoombaNet.Core;

namespace RoombaNet.Api.Services;

public class RoombaStatusService : BackgroundService
{
    private readonly IRoombaSubscriber _subscriber;
    private readonly ILogger<RoombaStatusService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<RoombaStatusUpdate> _statusChannel;

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

                // Try to write to channel, but don't block if channel is full
                _statusChannel.Writer.TryWrite(statusUpdate);

                _logger.LogDebug("Received status update on topic '{Topic}'", topic);
            }, stoppingToken);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Roomba Status Service is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Roomba Status Service encountered an error and will stop.");
            // Service will stop, let the host handle the restart policy
        }
        finally
        {
            _statusChannel.Writer.Complete();
            _logger.LogInformation("Roomba Status Service stopped.");
        }
    }
}
