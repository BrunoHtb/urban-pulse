using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

const string Host = "localhost";
const string User = "urbanpulse";
const string Password = "urbanpulse";
const string QueueName = "urbanpulse.events";

var factory = new ConnectionFactory()
{
    HostName = Host,
    UserName = User,
    Password = Password
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: QueueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

for (int i = 1; i <= 5; i++)
{
    var urbanEvent = new
    {
        Type = "Traffic",
        Description = $"Lentidão próxima ao Shopping Pátio Batel #{i}",
        Latitude = -25.4431,
        Longitude = -49.2800,
        Severity = i % 4 + 1,
        Timestamp = DateTime.UtcNow
    };

    var jsonMessage = JsonSerializer.Serialize(urbanEvent);
    var body = Encoding.UTF8.GetBytes(jsonMessage);

    var props = new BasicProperties {  Persistent = true };

    await channel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: QueueName,
        mandatory: false,
        basicProperties: props,
        body: body  );
    Console.WriteLine($"[Producer] Sent JSON: {jsonMessage}");
}

Console.WriteLine("Done. Press ENTER to exit.");
Console.ReadLine();