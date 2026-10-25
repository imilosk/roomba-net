using MQTTnet;

namespace RoombaNet.Transport.Mqtt;

public interface IRoombaConnectionManager : IDisposable, IAsyncDisposable
{
    bool IsConnected { get; }
    Task<IMqttClient> GetClient(CancellationToken cancellationToken = default);
    MqttClientSubscribeOptions CreateMqttSubscribeOptions(string topic);

    /// <summary>
    /// Asks the robot to publish its full device shadow now (it otherwise only emits the
    /// complete shadow periodically, serving partial pings in between). Call after subscribing
    /// so a snapshot is complete quickly.
    /// </summary>
    Task RequestShadow(CancellationToken cancellationToken = default);
}