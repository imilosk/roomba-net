using RoombaNet.Api.Endpoints;
using RoombaNet.Api.Services;
using RoombaNet.Core;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

// Add configuration sources to load secrets file
builder.Configuration
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile($"appsettings.{environment}.json", true)
    .AddJsonFile($"secrets.{environment}.json", true)
    .AddEnvironmentVariables()
    .Build();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add RoombaNet Core services
builder.Services.AddCore(builder.Configuration);

// Add API services
builder.Services.AddScoped<IRoombaApiService, RoombaApiService>();

// Add CORS if needed
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

// Map Roomba endpoints
app.MapRoombaEndpoints();

await app.RunAsync();