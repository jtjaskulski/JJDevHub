using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Infrastructure.ReadStore;
using JJDevHub.Content.Persistence;
using JJDevHub.Shared.Kernel.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace JJDevHub.Content.IntegrationTests.Fixtures;

public class ContentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("jjdevhub_content_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Disable the outbox Kafka publisher — no Kafka container in the test setup.
        // Outbox *writing* is still exercised: domain event handlers call IOutboxWriter.Enqueue,
        // which inserts rows into content.outbox_messages within the same PG transaction.
        // Only the background publishing to Kafka is skipped.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Outbox:PublisherEnabled"] = "false",
                    ["Vault:Enabled"] = "false",
                    ["Keycloak:auth-server-url"] = "",
                    ["MongoDb:ConnectionString"] = _mongo.GetConnectionString(),
                    ["MongoDb:DatabaseName"] = "jjdevhub_content_test_read"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ContentDbContext>>();
            services.AddDbContext<ContentDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.RemoveAll<IMongoClient>();
            services.AddSingleton<IMongoClient>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                return new MongoClient(opts.ConnectionString);
            });
            services.RemoveAll<IWorkExperienceReadStore>();
            services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();
            services.RemoveAll<ICurriculumVitaeReadStore>();
            services.AddSingleton<ICurriculumVitaeReadStore, MongoCurriculumVitaeReadStore>();

            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, NoOpEventBus>();
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _mongo.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
        await _mongo.StopAsync();
    }
}
