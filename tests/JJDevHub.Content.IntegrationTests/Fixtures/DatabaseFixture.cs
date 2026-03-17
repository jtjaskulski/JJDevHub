using JJDevHub.Content.Infrastructure.ReadStore;
using JJDevHub.Content.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.IntegrationTests.Fixtures;

public static class DatabaseFixture
{
    public static async Task ResetDatabaseAsync(ContentApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var mongoOpts = scope.ServiceProvider.GetRequiredService<IOptions<MongoDbSettings>>();
        var mongoClient = new MongoClient(mongoOpts.Value.ConnectionString);
        var db = mongoClient.GetDatabase(mongoOpts.Value.DatabaseName);
        try
        {
            await db.DropCollectionAsync("work_experiences");
        }
        catch (MongoException)
        {
            // kolekcja może nie istnieć przy pierwszym uruchomieniu
        }
    }
}
