namespace RoombaNet.Mqtt;

public interface IRoombaClient
{
    Task Find(CancellationToken cancellationToken = default);
    Task Start(CancellationToken cancellationToken = default);
    Task Stop(CancellationToken cancellationToken = default);
    Task Pause(CancellationToken cancellationToken = default);
    Task Resume(CancellationToken cancellationToken = default);
    Task Dock(CancellationToken cancellationToken = default);
    Task Evac(CancellationToken cancellationToken = default);
    Task Reset(CancellationToken cancellationToken = default);
    Task Train(CancellationToken cancellationToken = default);
    Task ChildLock(bool enable, CancellationToken cancellationToken = default);
}