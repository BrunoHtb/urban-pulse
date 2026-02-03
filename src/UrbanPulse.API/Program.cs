using Elastic.Clients.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var settings = new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200"))
    .DefaultIndex(builder.Configuration["Elasticsearch:DefaultIndex"] ?? "urban-events");

builder.Services.AddSingleton(new ElasticsearchClient(settings));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
