using MQTTnet;

namespace RoombaNet.Transport.Mqtt;

public interface IRoombaConnectionManager : IDisposable, IAsyncDisposable
{
    bool IsConnected { get; }
    Task<IMqttClient> GetClient(CancellationToken cancellationToken = default);
    MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic);
}