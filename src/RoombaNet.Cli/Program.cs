using Microsoft.Extensions.DependencyInjection;
using RoombaNet.Mqtt;
using Bootstrapper = RoombaNet.Cli.Bootstrapper;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var services = Bootstrapper.CreateServiceProvider();

var roombaController = services.GetRequiredService<IRoombaController>();

await roombaController.Subscribe();
// await roombaController.Find();

Console.WriteLine("Waiting for messages... (Press any key to exit)");
Console.ReadKey();
