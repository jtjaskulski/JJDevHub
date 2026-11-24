using JJDevHub.Gateway.Versions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

#pragma warning disable EXTEXP0001
builder.Services
    .AddHttpClient(VersionReport.HttpClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(2);
    })
    .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/version", (IConfiguration configuration) =>
{
    string version = configuration["SERVICE_VERSION"] ?? "";
    return Results.Text(version);
});

app.MapGet("/versions", async (
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    HttpClient httpClient = httpClientFactory.CreateClient(VersionReport.HttpClientName);
    VersionReport report = await VersionReport.CollectAsync(httpClient, configuration, cancellationToken);
    return Results.Ok(report);
});

app.MapReverseProxy();

app.Run();

public partial class Program
{
}