namespace RoombaNet.Mqtt;

public interface IRoombaClient
{
    Task Find();
    Task Start();
    Task Stop();
    Task Pause();
    Task Resume();
    Task Dock();
    Task Evac();
}