using MQTTnet;

namespace RoombaNet.Mqtt;

public interface IRoombaConnectionManager : IDisposable, IAsyncDisposable
{
    bool IsConnected { get; }
    Task<IMqttClient> GetClient(CancellationToken cancellationToken = default);
    MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic);
}