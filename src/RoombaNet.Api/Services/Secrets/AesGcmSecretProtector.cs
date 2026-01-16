using System.Security.Cryptography;
using System.Text;

namespace RoombaNet.Api.Services.Secrets;

public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string VersionPrefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesGcmSecretProtector(byte[] key)
    {
        if (key.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException("Encryption key must be 16, 24, or 32 bytes.", nameof(key));
        }

        _key = key;
    }

    public string? Protect(string? plaintext)
    {
        if (plaintext is null)
        {
            return null;
        }

        if (plaintext.Length == 0)
        {
            return string.Empty;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, cipherBytes, tag, null);

        var combined = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize + TagSize, cipherBytes.Length);

        return VersionPrefix + Convert.ToBase64String(combined);
    }

    public string? Unprotect(string? protectedValue)
    {
        if (protectedValue is null)
        {
            return null;
        }

        if (protectedValue.Length == 0)
        {
            return string.Empty;
        }

        if (!protectedValue.StartsWith(VersionPrefix, StringComparison.Ordinal))
        {
            return protectedValue;
        }

        var payload = protectedValue[VersionPrefix.Length..];
        var combined = Convert.FromBase64String(payload);

        if (combined.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException("Encrypted payload is too short.");
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[combined.Length - NonceSize - TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(combined, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plaintextBytes = new byte[cipherBytes.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plaintextBytes, null);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
