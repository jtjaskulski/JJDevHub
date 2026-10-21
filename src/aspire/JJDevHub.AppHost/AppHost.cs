using Aspire.Hosting.Docker.Resources.ComposeNodes;

using JJDevHub.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("jjdevhub")
    .WithProperties(environment => environment.DashboardEnabled = false)
    .ConfigureComposeFile(file =>
        file.AddNetwork(new Network { Name = "jjdevhub", External = true }));

var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithImageTag("16-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var newsWrite = postgres.AddDatabase(ResourceNames.NewsWrite, "news_write");
var newsRead = postgres.AddDatabase(ResourceNames.NewsRead, "news_read");
var jjdevhub = postgres.AddDatabase(ResourceNames.DefaultConnection, "jjdevhub");

var kafka = builder.AddKafka(ResourceNames.Kafka)
    .WithKafkaUI()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var jwtKey = builder.AddParameter("jwt-key", secret: true);

builder.AddProject<Projects.JJDevHub_Api>(ResourceNames.Api)
    .WithReference(jjdevhub)
    .WaitFor(jjdevhub)
    .WithEnvironment("Jwt__Key", jwtKey)
    .PublishAsDockerComposeService((_, service) => service.Networks.Add("jjdevhub"));

_ = newsWrite;
_ = newsRead;
_ = kafka;

builder.Build().Run();
