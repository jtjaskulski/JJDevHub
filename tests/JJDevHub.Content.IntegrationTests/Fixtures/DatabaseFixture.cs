using JJDevHub.Content.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Content.IntegrationTests.Fixtures;

public static class DatabaseFixture
{
    public static async Task ResetDatabaseAsync(ContentApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
