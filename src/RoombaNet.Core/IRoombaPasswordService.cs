namespace RoombaNet.Core;

public interface IRoombaPasswordService
{
    Task<string> GetPassword(string ipAddress, int port = 8883, CancellationToken cancellationToken = default);
}
