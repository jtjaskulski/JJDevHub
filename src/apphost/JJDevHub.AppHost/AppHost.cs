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