namespace RoombaNet.Core;

public interface IRoombaPasswordService
{
    Task<string> GetPassword(string ipAddress, int port = 8883, CancellationToken cancellationToken = default);
    Task<bool> SetPassword(
        string ipAddress,
        string password,
        string assetId,
        string assetType,
        int port = 8883,
        CancellationToken cancellationToken = default);
}
