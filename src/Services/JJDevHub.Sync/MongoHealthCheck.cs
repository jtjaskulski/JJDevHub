using JJDevHub.Content.Infrastructure.ReadStore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JJDevHub.Sync;

public sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoClient _client;
    private readonly IOptionsMonitor<MongoDbSettings> _settings;

    public MongoHealthCheck(IMongoClient client, IOptionsMonitor<MongoDbSettings> settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _client.GetDatabase(_settings.CurrentValue.DatabaseName);
            await db.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB ping failed.", ex);
        }
    }
}
