using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace RoombaNet.Proxy.Security;

/// <summary>
/// Produces the X.509 certificate used by the native TLS listener. Drop-in clients do not verify the
/// certificate (they treat the proxy like the robot, whose cert is self-signed), so an ephemeral
/// self-signed certificate is sufficient unless an explicit one is configured.
/// </summary>
public static class SelfSignedCertificateFactory
{
    /// <summary>
    /// Loads the configured PKCS#12 certificate, or generates a fresh self-signed certificate when none is set.
    /// </summary>
    public static X509Certificate2 CreateOrLoad(string? certificatePath, string? certificatePassword, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!File.Exists(certificatePath))
            {
                throw new FileNotFoundException($"Proxy TLS certificate not found at '{certificatePath}'.", certificatePath);
            }

            logger.LogInformation("Loading proxy TLS certificate from {Path}", certificatePath);
            return X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                certificatePassword,
                X509KeyStorageFlags.Exportable);
        }

        logger.LogInformation("Generating self-signed TLS certificate for the proxy listener");
        return Generate();
    }

    private static X509Certificate2 Generate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=RoombaNet Proxy",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.1")], // id-kp-serverAuth
            false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        // Round-trip through PKCS#12 so the private key is persisted in a form the TLS stack can use
        // for server authentication across platforms.
        var pfx = certificate.Export(X509ContentType.Pfx);
        return X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.Exportable);
    }
}
