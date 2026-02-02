using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using UrbanPulse.Shared;
using UrbanPulse.Producer.Models;

// JSON
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

string apiKey = config["TomTom:ApiKey"] ?? "";
string rabbitHost = config["RabbitMQ:Host"] ?? "localhost";

/* RABBITMQ */
var factory = new ConnectionFactory()
{
    HostName = rabbitHost,
    UserName = config["RabbitMQ:User"],
    Password = config["RabbitMQ:Password"]
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();
using var httpClient = new HttpClient();

var pontos = new[] {
    new { Nome = "Batel (Shopping)", Lat = -25.4431, Lon = -49.2811 },
    new { Nome = "Centro (Praça Tiradentes)", Lat = -25.4284, Lon = -49.2733 },
    new { Nome = "Linha Verde (Pinheirinho)", Lat = -25.5123, Lon = -49.2655 }
};

Console.WriteLine("[Collector] Iniciando coleta de fluxo de Curitiba...");

while (true)
{
    foreach (var p in pontos)
    {
        try
        {
            string url = string.Create(CultureInfo.InvariantCulture,
                $"https://api.tomtom.com/traffic/services/4/flowSegmentData/absolute/10/json?key={apiKey}&point={p.Lat},{p.Lon}");

            var root = await httpClient.GetFromJsonAsync<TomTomFlowRoot>(url);

            if (root?.FlowData != null)
            {
                var urbanEvent = new UrbanEvent
                {
                    Type = "TrafficFlow",
                    Description = $"Velocidade em {p.Nome}: {root.FlowData.CurrentSpeed} km/h",
                    Latitude = p.Lat,
                    Longitude = p.Lon,
                    Severity = root.FlowData.CurrentSpeed < (root.FlowData.FreeFlowSpeed * 0.5) ? 3 : 1,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(urbanEvent);
                var body = Encoding.UTF8.GetBytes(json);
                await channel.BasicPublishAsync(string.Empty, "urbanpulse.events", false, new BasicProperties { Persistent = true }, body);

                Console.WriteLine($"[Sent] {p.Nome}: {root.FlowData.CurrentSpeed}km/h (Fluxo Livre: {root.FlowData.FreeFlowSpeed}km/h)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Erro {p.Nome}] {ex.Message}");
        }
    }

    Console.WriteLine("\n--- Aguardando 5 minutos para nova coleta ---\n");
    await Task.Delay(TimeSpan.FromMinutes(5));
}
