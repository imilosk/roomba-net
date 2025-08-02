using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Transport.Tls;

public class RoombaPasswordClient : IRoombaPasswordClient
{
    private readonly ILogger<RoombaPasswordClient> _logger;
    private readonly RoombaSettings _roombaSettings;
    private int _sliceFrom = 9;

    public RoombaPasswordClient(
        ILogger<RoombaPasswordClient> logger,
        RoombaSettings roombaSettings
    )
    {
        _logger = logger;
        _roombaSettings = roombaSettings;
    }

    public async Task<string> GetPassword(CancellationToken cancellationToken = default)
    {
        var (sslStream, tcpClient) = await ConnectAsync(cancellationToken);
        await SendDiscoveryPacketAsync(sslStream, cancellationToken);
        var password = await ListenForPasswordAsync(sslStream, cancellationToken);

        sslStream.Dispose();
        tcpClient.Dispose();

        return password;
    }

    private async Task<(SslStream sslStream, TcpClient tcpClient)> ConnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to Roomba at {Ip}:{Port}...", _roombaSettings.Ip, _roombaSettings.Port);

        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(_roombaSettings.Ip, _roombaSettings.Port, cancellationToken);

        var sslStream = new SslStream(
            tcpClient.GetStream(),
            false,
            ValidateServerCertificate,
            null
        );

        var sslOptions = new SslClientAuthenticationOptions
        {
            TargetHost = _roombaSettings.Ip,
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            RemoteCertificateValidationCallback = ValidateServerCertificate,
        };

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            sslOptions.CipherSuitesPolicy = new CipherSuitesPolicy([
                TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
            ]);
        }

        await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken);
        _logger.LogDebug("TLS connection established");

        return (sslStream, tcpClient);
    }

    private static bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors
    )
    {
        // rejectUnauthorized: false - accept any certificate
        return true;
    }

    private async Task SendDiscoveryPacketAsync(SslStream sslStream, CancellationToken cancellationToken)
    {
        if (sslStream is null)
            throw new InvalidOperationException("Not connected");

        const string hexPacket = "f005efcc3b2900";
        var packetBytes = Convert.FromHexString(hexPacket);

        _logger.LogDebug("Sending discovery packet: {Packet}", hexPacket);

        await sslStream.WriteAsync(packetBytes, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);
    }

    private async Task<string> ListenForPasswordAsync(SslStream sslStream, CancellationToken cancellationToken)
    {
        if (sslStream is null)
            throw new InvalidOperationException("Not connected");

        var buffer = new byte[1024];
        var result = string.Empty;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await sslStream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    _logger.LogWarning("Connection closed by remote host");
                    result = string.Empty;
                    break;
                }

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var password = ProcessReceivedData(data);
                if (string.IsNullOrEmpty(password))
                {
                    continue;
                }

                result = password;
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from stream");
        }

        return result;
    }

    private string ProcessReceivedData(byte[] data)
    {
        _logger.LogDebug("Received {Length} bytes", data.Length);

        switch (data.Length)
        {
            case 2:
                _sliceFrom = 9;
                _logger.LogDebug("Received 2-byte response, setting slice position to 9");
                return string.Empty;
            case <= 7:
                _logger.LogError("Error getting password. Follow the instructions and try again.");
                throw new InvalidOperationException("Received data is too short to contain a password");
        }

        var passwordBytes = data.Skip(_sliceFrom).ToArray();
        var password = Encoding.UTF8.GetString(passwordBytes);

        return password;
    }
}
