using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Mqtt;
using Bootstrapper = RoombaNet.Cli.Bootstrapper;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var services = Bootstrapper.CreateServiceProvider();

var service = services.GetRequiredService<IService>();

await service.Main();
