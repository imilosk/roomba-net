namespace RoombaNet.Transport.Tls;

public interface IRoombaPasswordClient
{
    Task<string> GetPassword(string ipAddress, int port = 8883, CancellationToken cancellationToken = default);
}
