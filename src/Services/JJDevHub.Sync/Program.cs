using Confluent.Kafka;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Infrastructure.ReadStore;
using JJDevHub.Sync;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();
builder.Services.AddSingleton<IJobApplicationReadStore, MongoJobApplicationReadStore>();
builder.Services.AddSingleton<KafkaConsumerHealthState>();
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var bootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
    var cfg = new ProducerConfig { BootstrapServers = bootstrap };
    return new ProducerBuilder<string, string>(cfg).Build();
});

builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.AddHealthChecks()
    .AddCheck<KafkaConsumerHealthCheck>("kafka_consumer")
    .AddCheck<MongoHealthCheck>("mongo");

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/health"));
await app.RunAsync();
