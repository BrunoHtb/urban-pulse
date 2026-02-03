using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UrbanPulse.Shared;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

/*  CONFIG RABBITMQ */
string rabbitHost = config["RabbitMQ:Host"] ?? "localhost";
string rabbitUser = config["RabbitMQ:User"] ?? "guest";
string rabbitPassword = config["RabbitMQ:Password"] ?? "guest";
string rabbitQueue = config["RabbitMQ:Queue"] ?? "urbanpulse.events";

/*  CONFIG ELASTICSEARCH    */
string elasticsearchHost = config["Elasticsearch:Host"] ?? "http://localhost:9200";
string elasticsearchIndice = config["Elasticsearch:Indice"] ?? "urban-events";

/*  CONFIG MONGODB  */
string mongoHost = config["MongoDB:Host"] ?? "mongodb://localhost:27017";
string mongoDatabase = config["MongoDB:Database"] ?? "urbanpulseDB";
string mongoCollection = config["MongoDB:Collection"] ?? "Events";

/*  ELASTCHSEARCH   */
var elasticSettings = new ElasticsearchClientSettings(new Uri(elasticsearchHost))
    .DefaultIndex(elasticsearchIndice);

var elasticClient = new ElasticsearchClient(elasticSettings);
var existsResponse = await elasticClient.Indices.ExistsAsync(elasticsearchIndice);

if (!existsResponse.Exists)
{
    await elasticClient.Indices.CreateAsync(elasticsearchIndice, c => c
        .Mappings(m => m
            .Properties<UrbanEvent>(p => p
                .GeoPoint(n => n.Location)
            )
        )
    );
    Console.WriteLine("[Elastic] Índice 'urban-events' criado com Geo-Mapping.");
}

/*  MONGODB */
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
var mongoClient = new MongoClient(mongoHost);
var database = mongoClient.GetDatabase(mongoDatabase);
var collection = database.GetCollection<UrbanEvent>(mongoCollection);

/* RABBITMQ */
var factory = new ConnectionFactory()
{
    HostName = rabbitHost,
    UserName = rabbitUser,
    Password = rabbitPassword
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: rabbitQueue,
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
            // Salva no MongoDB
            await collection.InsertOneAsync(urbanEvent);
            Console.WriteLine($"[Mongo] Salvo: {urbanEvent.Id}");

            // Salva no Elasticsearch
            var elasticResponse = await elasticClient.IndexAsync(urbanEvent);
            if(elasticResponse.IsSuccess())
            {
                Console.WriteLine("[Elastic] Indexado com sucesso!");
            }
            else
            {
                Console.WriteLine($"[Elastic Erro] {elasticResponse.DebugInformation}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Erro] Falha ao processar mensagem: {ex.Message}");
    }
    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
};

await channel.BasicConsumeAsync(
    queue: rabbitQueue,
    autoAck: false,
    consumer: consumer);

Console.WriteLine(" [*] Aguardando mensagens. Pressione [enter] para sair.");
Console.ReadLine();