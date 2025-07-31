using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using RoombaNet.Mqtt;
using Bootstrapper = RoombaNet.Cli.Bootstrapper;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var services = Bootstrapper.CreateServiceProvider();

var roombaClient = services.GetRequiredService<IRoombaClient>();
var roombaSubscriber = services.GetRequiredService<IRoombaSubscriber>();

Console.WriteLine("Waiting for messages... (Press any key to exit)");
await roombaSubscriber.Subscribe(PrintMessage);
await roombaClient.Find();

Console.ReadKey();

return 0;

static void PrintMessage(MqttApplicationMessageReceivedEventArgs e)
{
    Console.WriteLine("📥 Message received:");
    Console.WriteLine($"   Topic: {e.ApplicationMessage.Topic}");
    var payload = e.ApplicationMessage.ConvertPayloadToString();
    Console.WriteLine($"   Payload: {payload}");
    Console.WriteLine($"   QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
    Console.WriteLine($"   Retain: {e.ApplicationMessage.Retain}");
    Console.WriteLine($"   Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine("   " + new string('-', 50));
}