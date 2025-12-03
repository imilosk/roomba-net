namespace RoombaNet.Core;

public interface IRoombaWifiService
{
    Task<bool> ConfigureWifiAsync(
        WifiConfigurationRequest request,
        CancellationToken cancellationToken = default
    );
}
