using MQTTnet;

namespace RoombaNet.Core;

public interface IRoombaSubscriber
{
    Task Subscribe(
        Action<MqttApplicationMessageReceivedEventArgs> onMessageReceived,
        CancellationToken cancellationToken = default
    );
}