namespace RoombaNet.Api.Services.RoombaClients;

public interface IRoombaClientFactory
{
    Task<RoombaClient> GetClient(string robotId, CancellationToken cancellationToken = default);
    bool RemoveClient(string robotId);
}
