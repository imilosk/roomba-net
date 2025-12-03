namespace RoombaNet.Core;

public interface IRoombaWifiClient
{
    Task<bool> ConfigureWifiAsync(
        string wifiSsid,
        string wifiPassword,
        string? robotName = null,
        string? timezone = null,
        string? country = null,
        CancellationToken cancellationToken = default
    );
}
