using MQTTnet;

namespace RoombaNet.Mqtt;

public interface IRoombaConnectionManager : IDisposable, IAsyncDisposable
{
    bool IsConnected { get; }
    Task<IMqttClient> GetClient();
    MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic);
}