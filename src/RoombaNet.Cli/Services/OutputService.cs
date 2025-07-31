using MQTTnet;

namespace RoombaNet.Cli.Services;

public class OutputService
{
    public static void PrintMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine("📥 Message received:");
        Console.WriteLine($"   Topic: {e.ApplicationMessage.Topic}");
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        Console.WriteLine($"   Payload: {payload}");
        Console.WriteLine($"   QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
        Console.WriteLine($"   Retain: {e.ApplicationMessage.Retain}");
        Console.WriteLine($"   Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine("   " + new string('-', 50));
    }
}