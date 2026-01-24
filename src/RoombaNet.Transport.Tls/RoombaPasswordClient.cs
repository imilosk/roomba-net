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
    private static readonly byte[] MarkerBytes = [0xEF, 0xCC, 0x3B, 0x29];
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

    public async Task<bool> SetPassword(
        string ipAddress,
        string password,
        string assetId,
        string assetType,
        int port = 8883,
        CancellationToken cancellationToken = default
    )
    {
        var (sslStream, tcpClient) = await ConnectAsync(ipAddress, port, cancellationToken);
        var connectPacket = BuildConnectPacket(assetType, assetId);
        _logger.LogDebug("Sending CONNECT packet: {Packet}", Convert.ToHexString(connectPacket).ToLowerInvariant());
        await sslStream.WriteAsync(connectPacket, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);

        var connack = await ReadResponseAsync(sslStream, "CONNACK", cancellationToken);
        if (!TryGetConnAckReturnCode(connack, out var returnCode))
        {
            _logger.LogWarning("Invalid CONNACK response.");
            await sslStream.DisposeAsync();
            tcpClient.Dispose();
            return false;
        }

        if (returnCode != 0x00)
        {
            _logger.LogWarning("CONNACK rejected with return code 0x{Code:x2}.", returnCode);
            await sslStream.DisposeAsync();
            tcpClient.Dispose();
            return false;
        }

        await SendUtcTimeAsync(sslStream, cancellationToken);
        await SendLocalTimeOffsetAsync(sslStream, cancellationToken);
        await SendTimezoneAsync(sslStream, cancellationToken);

        var packet = BuildSetPasswordPacket(password);
        _logger.LogDebug("Sending SET-PASSWORD packet: {Packet}", Convert.ToHexString(packet).ToLowerInvariant());

        await sslStream.WriteAsync(packet, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);

        var setPasswordResponse = await ReadResponseAsync(sslStream, "SET-PASSWORD response", cancellationToken);
        if (!TryParseSetPasswordResponse(setPasswordResponse, out var status, out var statusMessage))
        {
            _logger.LogWarning("Unable to parse SET-PASSWORD response.");
            await sslStream.DisposeAsync();
            tcpClient.Dispose();
            return false;
        }

        LogSetPasswordStatus(status, statusMessage);
        if (status != 0x00)
        {
            await sslStream.DisposeAsync();
            tcpClient.Dispose();
            return false;
        }

        await sslStream.DisposeAsync();
        tcpClient.Dispose();

        return true;
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

    private static byte[] BuildSetPasswordPacket(string password)
    {
        var passwordBytes = Encoding.ASCII.GetBytes(password);
        var payload = new byte[MarkerBytes.Length + passwordBytes.Length + 1];

        Buffer.BlockCopy(MarkerBytes, 0, payload, 0, MarkerBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, payload, MarkerBytes.Length, passwordBytes.Length);
        payload[^1] = 0x00;

        var remainingLength = payload.Length;
        var remainingLengthBytes = EncodeRemainingLength(remainingLength);

        var packet = new byte[1 + remainingLengthBytes.Length + payload.Length];
        packet[0] = 0xF0;
        Buffer.BlockCopy(remainingLengthBytes, 0, packet, 1, remainingLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, 1 + remainingLengthBytes.Length, payload.Length);

        return packet;
    }

    private static byte[] EncodeRemainingLength(int length)
    {
        var bytes = new System.Collections.Generic.List<byte>();
        var value = length;

        do
        {
            var digit = value % 128;
            value /= 128;
            if (value > 0)
            {
                digit |= 0x80;
            }
            bytes.Add((byte)digit);
        }
        while (value > 0);

        return bytes.ToArray();
    }

    private async Task<byte[]> ReadResponseAsync(
        SslStream sslStream,
        string label,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[2048];
        var bytesRead = await sslStream.ReadAsync(buffer, cancellationToken);

        if (bytesRead <= 0)
        {
            _logger.LogWarning("No {Label} received.", label);
            return Array.Empty<byte>();
        }

        var response = buffer[..bytesRead];
        _logger.LogDebug("Received {Label}: {Packet}", label, Convert.ToHexString(response).ToLowerInvariant());
        return response;
    }

    private static bool TryGetConnAckReturnCode(byte[] response, out byte returnCode)
    {
        // CONNACK: 0x20 0x02 0x00 <return code>
        if (response.Length < 4 || response[0] != 0x20 || response[1] != 0x02)
        {
            returnCode = 0xFF;
            return false;
        }

        returnCode = response[3];
        return true;
    }

    private static byte[] BuildConnectPacket(string assetType, string assetId)
    {
        var variableHeader = new List<byte>();
        variableHeader.AddRange(EncodeMqttString("MQTT"));
        variableHeader.Add(0x04);
        variableHeader.Add(0xC2);
        variableHeader.Add(0x00);
        variableHeader.Add(0x00);

        var payload = new List<byte>();
        payload.AddRange(EncodeMqttString(assetId));
        payload.AddRange(EncodeMqttString(assetId));
        payload.AddRange(EncodeMqttString(assetType));

        var bodyLength = variableHeader.Count + payload.Count;
        var packet = new List<byte> { 0x10 };
        packet.AddRange(EncodeRemainingLength(bodyLength));
        packet.AddRange(variableHeader);
        packet.AddRange(payload);

        return packet.ToArray();
    }

    private static byte[] EncodeMqttString(string value)
    {
        var data = Encoding.ASCII.GetBytes(value ?? string.Empty);
        var length = (ushort)data.Length;
        return [(byte)(length >> 8), (byte)(length & 0xFF), .. data];
    }

    private async Task SendTimezoneAsync(SslStream sslStream, CancellationToken cancellationToken)
    {
        var timezone = TimeZoneInfo.Local.Id;
        var payload = $"{{\"state\":{{\"timezone\":\"{timezone}\"}}}}";

        var packet = BuildPublishPacket("wifictl", payload);
        _logger.LogDebug("Sending timezone wifictl payload: {Payload}", payload);
        _logger.LogDebug("Sending timezone wifictl packet: {Packet}", Convert.ToHexString(packet).ToLowerInvariant());
        await sslStream.WriteAsync(packet, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);
    }

    private async Task SendUtcTimeAsync(SslStream sslStream, CancellationToken cancellationToken)
    {
        var utcTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{{\"state\":{{\"utctime\":{utcTime}}}}}";

        var packet = BuildPublishPacket("wifictl", payload);
        _logger.LogDebug("Sending utctime wifictl payload: {Payload}", payload);
        _logger.LogDebug("Sending utctime wifictl packet: {Packet}", Convert.ToHexString(packet).ToLowerInvariant());
        await sslStream.WriteAsync(packet, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);
    }

    private async Task SendLocalTimeOffsetAsync(SslStream sslStream, CancellationToken cancellationToken)
    {
        var offsetMinutes = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
        var payload = $"{{\"state\":{{\"localtimeoffset\":{offsetMinutes}}}}}";
        var packet = BuildPublishPacket("wifictl", payload);
        _logger.LogDebug("Sending localtimeoffset wifictl payload: {Payload}", payload);
        _logger.LogDebug("Sending localtimeoffset wifictl packet: {Packet}", Convert.ToHexString(packet).ToLowerInvariant());
        await sslStream.WriteAsync(packet, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);
    }

    private bool TryParseSetPasswordResponse(byte[] response, out byte status, out string message)
    {
        status = 0xFF;
        message = "Unknown response.";

        if (response.Length < MarkerBytes.Length + 1)
        {
            return false;
        }

        var markerIndex = FindMarkerIndex(response, MarkerBytes);
        if (markerIndex < 0 || markerIndex + MarkerBytes.Length >= response.Length)
        {
            return false;
        }

        status = response[markerIndex + MarkerBytes.Length];
        message = GetSetPasswordStatusMessage(status);
        return true;
    }

    private void LogSetPasswordStatus(byte status, string message)
    {
        if (status == 0x00)
        {
            _logger.LogInformation("SET-PASSWORD status 0x00: {Message}", message);
        }
        else
        {
            _logger.LogWarning("SET-PASSWORD status 0x{Status:x2}: {Message}", status, message);
        }
    }

    private static int FindMarkerIndex(byte[] data, byte[] marker)
    {
        for (var i = 0; i <= data.Length - marker.Length; i++)
        {
            var match = true;
            for (var j = 0; j < marker.Length; j++)
            {
                if (data[i + j] != marker[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetSetPasswordStatusMessage(byte status)
    {
        return status switch
        {
            0x00 => "Password accepted by robot.",
            0x01 => "Password rejected. Verify asset type/BLID context and password format (:1:<unix>:<16 chars>).",
            0x02 => "Robot reported internal error. Retry after power cycle or re-enter provisioning mode.",
            0x03 => "Permission denied. Ensure you're connected to SoftAP and robot is in provisioning mode.",
            _ => "Unknown status returned by robot.",
        };
    }

    private static byte[] BuildPublishPacket(string topic, string payload)
    {
        var topicBytes = EncodeMqttString(topic);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var remainingLength = topicBytes.Length + payloadBytes.Length;
        var header = new List<byte> { 0x30 };
        header.AddRange(EncodeRemainingLength(remainingLength));

        var packet = new byte[header.Count + remainingLength];
        header.CopyTo(packet, 0);
        Buffer.BlockCopy(topicBytes, 0, packet, header.Count, topicBytes.Length);
        Buffer.BlockCopy(payloadBytes, 0, packet, header.Count + topicBytes.Length, payloadBytes.Length);

        return packet;
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
