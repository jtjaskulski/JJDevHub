using System.Text.Json;

using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.EventStore.Postgres;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Api.Tests
{
    public sealed class ImporterTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public ImporterTests(ApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Importer_moves_aspnet_user_and_drops_table()
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
            var hash = hasher.HashPassword(new Account(), "Password1");
            var userId = Guid.NewGuid();
            await write.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "AspNetUsers" ("Id" text PRIMARY KEY, "Email" text, "PasswordHash" text);
                CREATE TABLE IF NOT EXISTS "AspNetRoles" ("Id" text PRIMARY KEY, "Name" text);
                CREATE TABLE IF NOT EXISTS "AspNetUserRoles" ("UserId" text, "RoleId" text);
                """);
            await write.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO "AspNetRoles" ("Id", "Name") VALUES ({"role-1"}, {"student"});
                INSERT INTO "AspNetUsers" ("Id", "Email", "PasswordHash") VALUES ({userId.ToString()}, {"imported@test.com"}, {hash});
                INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES ({userId.ToString()}, {"role-1"});
                """);

            var importer = scope.ServiceProvider.GetRequiredService<AccountImporter>();
            await importer.ImportAsync(CancellationToken.None);

            var events = await write.Set<EventRecord>()
                .Where(record => record.StreamId == $"account-{userId}")
                .ToListAsync();
            var registered = Assert.Single(events, record => record.EventType == "identity.account-registered.v1");
            using (var payload = JsonDocument.Parse(registered.Payload))
            {
                Assert.Equal(hash, payload.RootElement.GetProperty("passwordHash").GetString());
            }
            Assert.Contains(events, record =>
                record.EventType == "identity.role-granted.v1" && record.Payload.Contains("student", StringComparison.Ordinal));
            var exists = await write.Database.SqlQueryRaw<bool>("""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'AspNetUsers'
                ) AS "Value"
                """).SingleAsync();
            Assert.False(exists);
        }
    }
}
