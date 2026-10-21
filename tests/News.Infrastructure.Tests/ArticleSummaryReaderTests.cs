using JJDevHub.News.Application.Articles;
using JJDevHub.News.Infrastructure;
using JJDevHub.News.Infrastructure.Read;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace News.Infrastructure.Tests
{
    public sealed class ArticleSummaryReaderTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        [Fact]
        public async Task Find_returns_row()
        {
            var articleId = Guid.NewGuid();
            await InsertAsync(Row(articleId, "Nowy wpis", On(3)));

            var summary = await FindAsync(articleId);

            Assert.NotNull(summary);
            Assert.Equal("Nowy wpis", summary.Title);
        }

        [Fact]
        public async Task Find_missing_returns_null()
        {
            var summary = await FindAsync(Guid.NewGuid());

            Assert.Null(summary);
        }

        [Fact]
        public async Task List_orders_newest_first()
        {
            await InsertAsync(
                Row(Guid.NewGuid(), "Środkowy", On(2)),
                Row(Guid.NewGuid(), "Najstarszy", On(1)),
                Row(Guid.NewGuid(), "Najnowszy", On(3)));

            var page = await ListAsync(0, 10);

            Assert.Equal(["Najnowszy", "Środkowy", "Najstarszy"], page.Select(summary => summary.Title));
        }

        [Fact]
        public async Task List_pages()
        {
            await InsertAsync(
                Row(Guid.NewGuid(), "Środkowy", On(2)),
                Row(Guid.NewGuid(), "Najstarszy", On(1)),
                Row(Guid.NewGuid(), "Najnowszy", On(3)));

            var page = await ListAsync(2, 10);

            Assert.Equal(["Najstarszy"], page.Select(summary => summary.Title));
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await using var provider = CreateProvider();
            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            await read.Database.MigrateAsync();
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        private ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddNewsRead(_postgres.GetConnectionString());
            return services.BuildServiceProvider();
        }

        private async Task InsertAsync(params ArticleSummaryRow[] rows)
        {
            await using var provider = CreateProvider();
            await using var scope = provider.CreateAsyncScope();
            var read = scope.ServiceProvider.GetRequiredService<NewsReadContext>();
            read.ArticleSummaries.AddRange(rows);
            await read.SaveChangesAsync();
        }

        private async Task<ArticleSummary?> FindAsync(Guid articleId)
        {
            await using var provider = CreateProvider();
            await using var scope = provider.CreateAsyncScope();
            var reader = scope.ServiceProvider.GetRequiredService<IArticleSummaryReader>();
            return await reader.FindAsync(articleId, CancellationToken.None);
        }

        private async Task<IReadOnlyList<ArticleSummary>> ListAsync(int skip, int take)
        {
            await using var provider = CreateProvider();
            await using var scope = provider.CreateAsyncScope();
            var reader = scope.ServiceProvider.GetRequiredService<IArticleSummaryReader>();
            return await reader.ListAsync(skip, take, CancellationToken.None);
        }

        private static ArticleSummaryRow Row(Guid articleId, string title, DateTimeOffset createdOn) =>
            new()
            {
                ArticleId = articleId,
                Title = title,
                Body = "cześć",
                CreatedOn = createdOn,
                Version = 1,
            };

        private static DateTimeOffset On(int day) => new(2026, 1, day, 10, 0, 0, TimeSpan.Zero);
    }
}