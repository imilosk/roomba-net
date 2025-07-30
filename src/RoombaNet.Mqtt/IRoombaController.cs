namespace RoombaNet.Mqtt;

public interface IRoombaController : IDisposable, IAsyncDisposable
{
    Task Subscribe();
    Task Find();
}