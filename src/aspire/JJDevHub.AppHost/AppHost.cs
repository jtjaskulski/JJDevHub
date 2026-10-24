using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

using JJDevHub.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("jjdevhub")
    .WithProperties(environment => environment.DashboardEnabled = false)
    .ConfigureComposeFile(file =>
        file.AddNetwork(new Network { Name = "jjdevhub", External = true }));

var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithImageTag("16-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsDockerComposeService((_, service) => LimitMemory(service, "512M"));

var newsWrite = postgres.AddDatabase(ResourceNames.NewsWrite, "news_write");
var newsRead = postgres.AddDatabase(ResourceNames.NewsRead, "news_read");
var identityWrite = postgres.AddDatabase(ResourceNames.IdentityWrite, "identity_write");
var identityRead = postgres.AddDatabase(ResourceNames.IdentityRead, "identity_read");
var notificationsDb = postgres.AddDatabase(ResourceNames.Notifications, "notifications");

var kafka = builder.AddKafka(ResourceNames.Kafka)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
if (!builder.ExecutionContext.IsPublishMode)
{
    kafka = kafka.WithKafkaUI();
}

kafka = kafka.PublishAsDockerComposeService((_, service) =>
{
    LimitMemory(service, "768M");
    service.AddEnvironmentalVariable("KAFKA_HEAP_OPTS", "-Xms256m -Xmx512m");
});

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
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Networks.Add("jjdevhub");
        LimitMemory(service, "256M");
    });

identityApi.WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"));

var newsApi = builder.AddProject<Projects.News_API>(ResourceNames.NewsApi)
    .WithReference(newsWrite).WaitFor(newsWrite)
    .WithReference(newsRead).WaitFor(newsRead)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"))
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Networks.Add("jjdevhub");
        LimitMemory(service, "256M");
    });

builder.AddProject<Projects.Notifications_Worker>(ResourceNames.NotificationsWorker)
    .WithReference(notificationsDb).WaitFor(notificationsDb)
    .WithReference(kafka).WaitFor(kafka)
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Networks.Add("jjdevhub");
        LimitMemory(service, "256M");
    });

builder.AddProject<Projects.JJDevHub_Gateway>(ResourceNames.Gateway)
    .WithReference(identityApi)
    .WithReference(newsApi)
    .WithHttpEndpoint(port: 5080, targetPort: 8081, name: "http")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Networks.Add("jjdevhub");
        LimitMemory(service, "256M");
    });

static void LimitMemory(Service service, string memory)
{
    service.Deploy ??= new Deploy();
    service.Deploy.Resources ??= new Resources();
    service.Deploy.Resources.Limits ??= new ResourceSpec();
    service.Deploy.Resources.Limits.Memory = memory;
}

builder.Build().Run();
