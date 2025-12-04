using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RoombaNet.Transport.Tls;

public class RoombaPasswordClient : IRoombaPasswordClient
{
    private readonly ILogger<RoombaPasswordClient> _logger;
    private const int SliceFrom = 9;
    private const string HexPacket = "f005efcc3b2900";
    private static readonly byte[] PacketBytes = Convert.FromHexString(HexPacket);

    public RoombaPasswordClient(ILogger<RoombaPasswordClient> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetPassword(
        string ipAddress,
        int port = 8883,
        CancellationToken cancellationToken = default
    )
    {
        var (sslStream, tcpClient) = await ConnectAsync(ipAddress, port, cancellationToken);
        await SendDiscoveryPacketAsync(sslStream, cancellationToken);
        var password = await ListenForPasswordAsync(sslStream, cancellationToken);

        await sslStream.DisposeAsync();
        tcpClient.Dispose();

        return password;
    }

    private async Task<(SslStream sslStream, TcpClient tcpClient)> ConnectAsync(
        string ip,
        int port,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Connecting to Roomba at {Ip}:{Port}...", ip, port);

        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(ip, port, cancellationToken);

        var sslStream = new SslStream(
            tcpClient.GetStream(),
            false
        );

        var sslOptions = new SslClientAuthenticationOptions
        {
            TargetHost = ip,
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

        _logger.LogDebug("Sending discovery packet: {Packet}", HexPacket);

        await sslStream.WriteAsync(PacketBytes, cancellationToken);
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
        catch (InvalidOperationException)
        {
            // Roomba didn't send password - return empty string
            result = string.Empty;
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
                _logger.LogDebug("Received 2-byte response, waiting for password data");
                return string.Empty;
            case <= 7:
                throw new InvalidOperationException(
                    "Roomba did not send password. Make sure Roomba is on the dock, " +
                    "hold HOME button for 2 seconds until you hear beeps, " +
                    "then run this command immediately."
                );
        }

        var passwordBytes = data.Skip(SliceFrom).ToArray();
        var password = Encoding.UTF8.GetString(passwordBytes);

        return password;
    }
}
