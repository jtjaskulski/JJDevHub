using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(false);

IResourceBuilder<ParameterResource> serviceVersion = builder.AddParameter("service-version");
string repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".."));
string gatewayName = Required(builder.Configuration, "Gateway:Name");
string gatewayImage = Required(builder.Configuration, "Gateway:Image");
string gatewayDockerfile = Required(builder.Configuration, "Gateway:Dockerfile");
string gatewayProject = Required(builder.Configuration, "Gateway:Project");
string gatewayAssembly = Required(builder.Configuration, "Gateway:Assembly");
string learningName = Required(builder.Configuration, "Learning:Name");
string learningMigrateName = Required(builder.Configuration, "Learning:MigrateName");
string learningImage = Required(builder.Configuration, "Learning:Image");
string learningDockerfile = Required(builder.Configuration, "Learning:Dockerfile");
string learningProject = Required(builder.Configuration, "Learning:Project");
string learningAssembly = Required(builder.Configuration, "Learning:Assembly");
int gatewayPort = RequiredPort(builder.Configuration, "Gateway:Port");
string gatewayHost = Required(builder.Configuration, "Gateway:Host");
string telemetryName = Required(builder.Configuration, "Telemetry:Name");
string telemetryImage = Required(builder.Configuration, "Telemetry:Image");
string telemetryTag = Required(builder.Configuration, "Telemetry:Tag");
int grpcPort = RequiredPort(builder.Configuration, "Telemetry:GrpcPort");
int httpPort = RequiredPort(builder.Configuration, "Telemetry:HttpPort");
int grafanaPort = RequiredPort(builder.Configuration, "Telemetry:GrafanaPort");
string telemetryHost = Required(builder.Configuration, "Telemetry:Host");
List<IConfigurationSection> telemetryMounts = [];
foreach (IConfigurationSection mount in builder.Configuration.GetSection("Telemetry:Mounts").GetChildren())
{
    Required(mount, "Source");
    Required(mount, "Target");
    telemetryMounts.Add(mount);
}

if (telemetryMounts.Count == 0)
{
    throw new InvalidOperationException("Brak Telemetry:Mounts.");
}

builder.AddContainer(telemetryName, telemetryImage, telemetryTag)
    .WithEndpoint(targetPort: grpcPort, name: "otlp-grpc", isExternal: false, isProxied: false)
    .WithEndpoint(targetPort: httpPort, name: "otlp-http", isExternal: false, isProxied: false)
    .WithHttpEndpoint(targetPort: grafanaPort, port: grafanaPort, name: "grafana", isProxied: false)
    .PublishAsDockerComposeService((_, service) => BindTelemetryOnLoopback(service));

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "18")
    .WithDataVolume("jjdevhub-postgres")
    .WithInitFiles(Path.Combine(repositoryRoot, "deploy", "postgres"));
IResourceBuilder<ParameterResource> learningWritePassword = DatabasePassword(builder, "learning_write");
IResourceBuilder<ParameterResource> learningReadPassword = DatabasePassword(builder, "learning_read");
foreach (string databaseName in DatabaseNames())
{
    postgres = postgres.WithEnvironment(
        PasswordEnvironment(databaseName),
        PasswordFor(builder, databaseName, learningWritePassword, learningReadPassword));
}

postgres.PublishAsDockerComposeService((_, service) => BindPostgres(service));

IResourceBuilder<IResourceWithConnectionString> learningWrite = builder.AddConnectionString(
    "learning-write",
    ReferenceExpression.Create(
        $"Host={postgres.Resource.Host};Port={postgres.Resource.Port};Username=learning_write_owner;Password={learningWritePassword};Database=learning_write"));
IResourceBuilder<IResourceWithConnectionString> learningRead = builder.AddConnectionString(
    "learning-read",
    ReferenceExpression.Create(
        $"Host={postgres.Resource.Host};Port={postgres.Resource.Port};Username=learning_read_owner;Password={learningReadPassword};Database=learning_read"));

builder.AddProject<Projects.Learning_Api>(learningName)
    .WithReference(learningWrite)
    .WithReference(learningRead)
    .WithEnvironment("SERVICE_VERSION", serviceVersion)
    .WithEnvironment("OTEL_SERVICE_NAME", learningName)
    .WaitFor(postgres)
    .PublishAsDockerFile(container => ConfigureLearningImage(container, false));

builder.AddProject<Projects.Learning_Api>(learningMigrateName)
    .WithArgs("migrate")
    .WithExplicitStart()
    .WithReference(learningWrite)
    .WaitFor(postgres)
    .PublishAsDockerFile(container => ConfigureLearningImage(container, true));

builder.AddProject<Projects.JJDevHub_Gateway>(gatewayName)
    .WithEnvironment("SERVICE_VERSION", serviceVersion)
    .WithEnvironment("OTEL_SERVICE_NAME", gatewayName)
    .PublishAsDockerFile(container => ConfigureGatewayImage(container));

builder.Build().Run();

string Required(IConfiguration configuration, string key)
{
    string? value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Brak {key}.");
    }

    return value;
}

int RequiredPort(IConfiguration configuration, string key)
{
    string text = Required(configuration, key);
    if (!int.TryParse(text, out int port))
    {
        throw new InvalidOperationException($"Zły port {key}.");
    }

    return port;
}

void ConfigureLearningImage(IResourceBuilder<ContainerResource> container, bool migrate)
{
    container.WithDockerfile(repositoryRoot, learningDockerfile);
    container.WithBuildArg("PROJECT", learningProject);
    container.WithBuildArg("ASSEMBLY", learningAssembly);
    container.PublishAsDockerComposeService((resource, service) =>
        BindLearningOnCompose(resource, service, migrate));
}

void BindLearningOnCompose(DockerComposeServiceResource resource, Service service, bool migrate)
{
    service.Image = learningImage;
    service.Build = new Build
    {
        Context = repositoryRoot,
        Dockerfile = learningDockerfile,
        Args = new Dictionary<string, string>
        {
            ["PROJECT"] = learningProject,
            ["ASSEMBLY"] = learningAssembly,
        },
    };
    service.Entrypoint = ["dotnet", "/app/entrypoint.dll"];
    service.Ports = [];
    service.Environment["SERVICE_VERSION"] = serviceVersion.AsEnvironmentPlaceholder(resource);
    service.Environment["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://{telemetryName}:{grpcPort}";
    service.Environment["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
    service.DependsOn[telemetryName] = new ServiceDependency { Condition = "service_started" };
    service.DependsOn["postgres"] = new ServiceDependency { Condition = "service_started" };
    if (!migrate)
    {
        return;
    }

    service.Command = ["migrate"];
    service.Restart = "no";
}

void ConfigureGatewayImage(IResourceBuilder<ContainerResource> container)
{
    container.WithDockerfile(repositoryRoot, gatewayDockerfile);
    container.WithBuildArg("PROJECT", gatewayProject);
    container.WithBuildArg("ASSEMBLY", gatewayAssembly);
    container.WithEndpoint("http", BindGatewayHttp);
    container.PublishAsDockerComposeService((resource, service) =>
        BindGatewayOnLoopback(resource, service));
}

void BindGatewayHttp(EndpointAnnotation endpoint)
{
    endpoint.Port = gatewayPort;
    endpoint.TargetPort = gatewayPort;
    endpoint.TargetHost = "+";
}

void BindGatewayOnLoopback(DockerComposeServiceResource resource, Service service)
{
    service.Image = gatewayImage;
    service.Build = new Build
    {
        Context = repositoryRoot,
        Dockerfile = gatewayDockerfile,
        Args = new Dictionary<string, string>
        {
            ["PROJECT"] = gatewayProject,
            ["ASSEMBLY"] = gatewayAssembly,
        },
    };
    service.Ports = [$"{gatewayHost}:{gatewayPort}:{gatewayPort}"];
    service.Environment["SERVICE_VERSION"] = serviceVersion.AsEnvironmentPlaceholder(resource);
    service.Environment["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://{telemetryName}:{grpcPort}";
    service.Environment["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
    service.DependsOn[telemetryName] = new ServiceDependency { Condition = "service_started" };
}

void BindTelemetryOnLoopback(Service service)
{
    service.Image = $"{telemetryImage}:{telemetryTag}";
    service.ContainerName = telemetryName;
    service.Ports = [$"{telemetryHost}:{grafanaPort}:{grafanaPort}"];
    service.Expose = [grpcPort.ToString(), httpPort.ToString()];
    foreach (IConfigurationSection mount in telemetryMounts)
    {
        MountReadOnly(service, Required(mount, "Source"), Required(mount, "Target"));
    }
}

static void BindPostgres(Service service)
{
    service.Ports = [];
    if (service.Volumes is null)
    {
        return;
    }

    foreach (Volume volume in service.Volumes)
    {
        if (volume.Target is null)
        {
            continue;
        }

        if (!volume.Target.StartsWith("/docker-entrypoint-initdb.d", StringComparison.Ordinal))
        {
            continue;
        }

        volume.Type = "bind";
        volume.Source = "../deploy/postgres";
        volume.Target = "/docker-entrypoint-initdb.d";
        volume.ReadOnly = true;
    }
}

static string[] DatabaseNames()
{
    return
    [
        "identity_write",
        "identity_read",
        "news_write",
        "news_read",
        "learning_write",
        "learning_read",
        "cv_write",
        "cv_read",
        "contact_write",
        "contact_read",
    ];
}

static IResourceBuilder<ParameterResource> PasswordFor(
    IDistributedApplicationBuilder builder,
    string databaseName,
    IResourceBuilder<ParameterResource> learningWritePassword,
    IResourceBuilder<ParameterResource> learningReadPassword)
{
    if (databaseName == "learning_write")
    {
        return learningWritePassword;
    }

    if (databaseName == "learning_read")
    {
        return learningReadPassword;
    }

    return DatabasePassword(builder, databaseName);
}

static string PasswordEnvironment(string databaseName)
{
    return databaseName.ToUpperInvariant() + "_PASSWORD";
}

static IResourceBuilder<ParameterResource> DatabasePassword(IDistributedApplicationBuilder builder, string databaseName)
{
    string parameterName = databaseName.Replace('_', '-') + "-password";
    ParameterResource password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
        builder,
        parameterName,
        special: false);
    return builder.CreateResourceBuilder(password);
}

static void MountReadOnly(Service service, string source, string target)
{
    service.AddVolume(new Volume
    {
        Name = source,
        Type = "bind",
        Source = source,
        Target = target,
        ReadOnly = true,
    });
}