using RoombaNet.Api.Endpoints;
using RoombaNet.Api.Services;
using RoombaNet.Core;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

builder.Configuration
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile($"appsettings.{environment}.json", true)
    .AddJsonFile($"secrets.{environment}.json", true)
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCore(builder.Configuration);

builder.Services.AddScoped<IRoombaApiService, RoombaApiService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors();

app.MapRoombaEndpoints();

await app.RunAsync();