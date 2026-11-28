using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace JJDevHub.ServiceDefaults.Tests;

internal static class ServiceDefaultsHost
{
    public static Task<WebApplication> StartAsync(CancellationToken cancellationToken)
    {
        return StartAsync(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "",
        }, cancellationToken);
    }

    public static async Task<WebApplication> StartAsync(
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = "JJDevHub.ServiceDefaults.Tests",
        });

        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddServiceDefaults();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync(cancellationToken);
        return app;
    }
}