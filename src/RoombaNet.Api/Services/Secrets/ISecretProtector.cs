namespace RoombaNet.Api.Services.Secrets;

public interface ISecretProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? protectedValue);
}
