namespace RoombaNet.Api.Services.Secrets;

public sealed class NoOpSecretProtector : ISecretProtector
{
    public string? Protect(string? plaintext) => plaintext;
    public string? Unprotect(string? protectedValue) => protectedValue;
}
