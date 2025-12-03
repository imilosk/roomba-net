using MQTTnet;

namespace RoombaNet.Core;

public interface IRoombaSubscriptionService
{
    Task Subscribe(
        Action<MqttApplicationMessageReceivedEventArgs> onMessageReceived,
        CancellationToken cancellationToken = default
    );
}