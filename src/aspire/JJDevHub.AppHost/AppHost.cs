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
var identityWrite = postgres.AddDatabase(ResourceNames.IdentityWrite, "identity_write");
var identityRead = postgres.AddDatabase(ResourceNames.IdentityRead, "identity_read");

var kafka = builder.AddKafka(ResourceNames.Kafka)
    .WithKafkaUI()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var signingKeyPath = builder.AddParameter("signing-key-path");
var ownerEmail = builder.AddParameter("owner-email");

var identityApi = builder.AddProject<Projects.Identity_API>(ResourceNames.IdentityApi)
    .WithHttpEndpoint(port: 5082, targetPort: 8080, name: "http")
    .WithReference(identityWrite).WaitFor(identityWrite)
    .WithReference(identityRead).WaitFor(identityRead)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__SigningKeyPath", signingKeyPath)
    .WithEnvironment("Identity__OwnerEmail", ownerEmail)
    .WithEnvironment("Identity__Audience", "JJDevHub")
    .PublishAsDockerComposeService((_, service) => service.Networks.Add("jjdevhub"));

identityApi.WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"));

var newsApi = builder.AddProject<Projects.News_API>(ResourceNames.NewsApi)
    .WithReference(newsWrite).WaitFor(newsWrite)
    .WithReference(newsRead).WaitFor(newsRead)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"))
    .PublishAsDockerComposeService((_, service) => service.Networks.Add("jjdevhub"));

builder.AddProject<Projects.JJDevHub_Gateway>(ResourceNames.Gateway)
    .WithReference(identityApi)
    .WithReference(newsApi)
    .WithHttpEndpoint(port: 5080, targetPort: 8081, name: "http")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((_, service) => service.Networks.Add("jjdevhub"));

builder.Build().Run();
