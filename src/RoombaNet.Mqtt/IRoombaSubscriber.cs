using MQTTnet;

namespace RoombaNet.Mqtt;

public interface IRoombaSubscriber
{
    Task Subscribe(Action<MqttApplicationMessageReceivedEventArgs> onMessageReceived);
}