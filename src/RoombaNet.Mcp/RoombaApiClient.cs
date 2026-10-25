using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RoombaNet.Mcp;

/// <summary>
/// Thin HTTP client over the RoombaNet API. Returns raw JSON strings so MCP tool
/// results pass the device state straight through to the AI client.
/// </summary>
public class RoombaApiClient
{
    private readonly HttpClient _http;

    public RoombaApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetJson(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, cancellationToken);
        return await ReadBody(response, cancellationToken);
    }

    public async Task<string> PostJson(string path, object? body, CancellationToken cancellationToken)
    {
        using var content = body is null
            ? null
            : new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(path, content, cancellationToken);
        return await ReadBody(response, cancellationToken);
    }

    public async Task<string> DeleteJson(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.DeleteAsync(path, cancellationToken);
        return await ReadBody(response, cancellationToken);
    }

    private static async Task<string> ReadBody(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return string.IsNullOrWhiteSpace(body) ? "{\"success\":true}" : body;
        }

        return $"{{\"success\":false,\"status\":{(int)response.StatusCode},\"body\":{JsonSerializer.Serialize(body)}}}";
    }

    public static string Escape(string value) => Uri.EscapeDataString(value);
}
