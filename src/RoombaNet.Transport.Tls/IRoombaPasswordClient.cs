namespace RoombaNet.Transport.Tls;

public interface IRoombaPasswordClient
{
    Task<string> GetPassword(CancellationToken cancellationToken = default);
}
