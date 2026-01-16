using RoombaNet.Api.Endpoints;
using RoombaNet.Api.Services;
using RoombaNet.Api.Services.RoombaClients;
using RoombaNet.Api.Services.RoombaStatus;
using RoombaNet.Api.Services.RobotRegistry;
using RoombaNet.Core;
using Scalar.AspNetCore;
using RoombaNet.Transport.Mqtt;

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

builder.Services.AddCore(builder.Configuration, requireSettings: false);
builder.Services.AddMqtt(builder.Configuration);
builder.Services.AddRobotRegistry(builder.Configuration);

builder.Services.AddScoped<RoombaApiService>();
builder.Services.AddSingleton<IRoombaClientFactory, RoombaClientFactory>();
builder.Services.AddSingleton<RoombaStatusManager>();

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
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors();

app.MapRoombaEndpoints();

await app.RunAsync();
