using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(false);

IResourceBuilder<ParameterResource> serviceVersion = builder.AddParameter("service-version");
string repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".."));

builder.AddProject<Projects.JJDevHub_Gateway>("gateway")
    .WithEnvironment("SERVICE_VERSION", serviceVersion)
    .PublishAsDockerFile(container => ConfigureGatewayImage(container, repositoryRoot, serviceVersion));

builder.Build().Run();

static void ConfigureGatewayImage(
    IResourceBuilder<ContainerResource> container,
    string repositoryRoot,
    IResourceBuilder<ParameterResource> serviceVersion)
{
    container.WithDockerfile(repositoryRoot, "deploy/docker/Dockerfile");
    container.WithBuildArg("PROJECT", "JJDevHub.Gateway");
    container.WithBuildArg("ASSEMBLY", "JJDevHub.Gateway.dll");
    container.WithEndpoint("http", BindGatewayHttp);
    container.PublishAsDockerComposeService((resource, service) =>
        BindGatewayOnLoopback(resource, service, serviceVersion, repositoryRoot));
}

static void BindGatewayHttp(EndpointAnnotation endpoint)
{
    endpoint.Port = 8080;
    endpoint.TargetPort = 8080;
    endpoint.TargetHost = "+";
}

static void BindGatewayOnLoopback(
    DockerComposeServiceResource resource,
    Service service,
    IResourceBuilder<ParameterResource> serviceVersion,
    string repositoryRoot)
{
    service.Image = "jjdevhub-gateway:dev";
    service.Build = new Build
    {
        Context = repositoryRoot,
        Dockerfile = "deploy/docker/Dockerfile",
        Args = new Dictionary<string, string>
        {
            ["PROJECT"] = "JJDevHub.Gateway",
            ["ASSEMBLY"] = "JJDevHub.Gateway.dll",
        },
    };
    service.Ports = ["127.0.0.1:8080:8080"];
    service.Environment["SERVICE_VERSION"] = serviceVersion.AsEnvironmentPlaceholder(resource);
}