using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoombaNet.Mcp;

// RoombaNet MCP server: exposes local, cloud-free Roomba control as MCP tools.
// It bridges to the RoombaNet HTTP API (default http://localhost:5264), overridable
// with the ROOMBANET_API_URL environment variable.

var builder = Host.CreateApplicationBuilder(args);

// MCP talks over stdio, so all logging must go to stderr to avoid corrupting the protocol.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var apiBaseUrl = Environment.GetEnvironmentVariable("ROOMBANET_API_URL") ?? "http://localhost:5264";

builder.Services.AddHttpClient<RoombaApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
