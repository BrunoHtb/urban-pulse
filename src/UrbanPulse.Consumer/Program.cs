using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UrbanPulse.Shared;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("urbanpulseDB");
var collection = database.GetCollection<UrbanEvent>("Events");    

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

await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (sender, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    try
    {
        var urbanEvent = JsonSerializer.Deserialize<UrbanEvent>(message);
        
        if (urbanEvent != null)
        {
            Console.WriteLine($"[Consumer] Gravando no Mongo: {urbanEvent.Description}");

            await collection.InsertOneAsync(urbanEvent);

            Console.WriteLine($"[Consumer] Sucesso! ID: {urbanEvent.Id}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Erro] Falha ao processar mensagem: {ex.Message}");
    }
    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
};

await channel.BasicConsumeAsync(
    queue: QueueName,
    autoAck: false,
    consumer: consumer);

Console.WriteLine(" [*] Aguardando mensagens. Pressione [enter] para sair.");
Console.ReadLine();