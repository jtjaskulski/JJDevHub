using JJDevHub.Api.Domain;
using JJDevHub.Domain;
using JJDevHub.EventStore;

using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Api.Data
{
    public sealed class AccountImporter(IdentityWriteContext context, IEventStore store, TimeProvider time)
    {
        public async Task ImportAsync(CancellationToken cancellationToken)
        {
            if (!await TableExistsAsync(cancellationToken) || await context.AccountEmails.AnyAsync(cancellationToken))
            {
                return;
            }

            await context.Database.BeginTransactionAsync(cancellationToken);
            bool completed = false;
            try
            {
                await ImportUsersAsync(cancellationToken);
                await DropIdentityTablesAsync(cancellationToken);
                await context.Database.CommitTransactionAsync(cancellationToken);
                completed = true;
            }
            finally
            {
                if (!completed && context.Database.CurrentTransaction is not null)
                {
                    await context.Database.RollbackTransactionAsync(cancellationToken);
                }
            }
        }

        private static IEnumerable<string> DistinctRoles(IReadOnlyList<LegacyUser> rows)
        {
            var roles = new List<string>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.RoleName) || roles.Contains(row.RoleName))
                {
                    continue;
                }

                roles.Add(row.RoleName);
            }

            return roles;
        }

        private async Task<bool> TableExistsAsync(CancellationToken cancellationToken)
        {
            var exists = await context.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'AspNetUsers'
                    ) AS "Value"
                    """)
                .SingleAsync(cancellationToken);
            return exists;
        }

        private async Task ImportUsersAsync(CancellationToken cancellationToken)
        {
            var rows = await context.Database.SqlQueryRaw<LegacyUser>("""
                SELECT u."Id" AS "Id",
                       u."Email" AS "Email",
                       u."PasswordHash" AS "PasswordHash",
                       r."Name" AS "RoleName"
                FROM "AspNetUsers" u
                LEFT JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
                LEFT JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
                """).ToListAsync(cancellationToken);

            foreach (var group in rows.GroupBy(row => row.Id))
            {
                await ImportUserAsync(group.ToList(), cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task ImportUserAsync(IReadOnlyList<LegacyUser> rows, CancellationToken cancellationToken)
        {
            var first = rows[0];
            var email = EmailAddress.Normalize(first.Email);
            if (email is null || string.IsNullOrWhiteSpace(first.PasswordHash))
            {
                return;
            }

            var accountId = Guid.TryParse(first.Id, out var parsed) ? parsed : Guid.NewGuid();
            var displayName = DisplayName.FromEmail(email, accountId);
            var occurred = time.GetUtcNow();
            var events = new List<IDomainEvent>
            {
                new AccountRegisteredDomainEvent(
                    Guid.NewGuid(),
                    occurred,
                    accountId,
                    email,
                    first.PasswordHash,
                    displayName.Value),
            };
            foreach (var role in DistinctRoles(rows))
            {
                events.Add(new RoleGrantedDomainEvent(Guid.NewGuid(), occurred, accountId, role));
            }

            var saved = await store.AppendAsync(
                $"account-{accountId}",
                0,
                events,
                accountId.ToString(),
                cancellationToken);
            if (!saved.Succeeded)
            {
                throw new InvalidOperationException($"Could not import account {accountId}.");
            }

            context.AccountEmails.Add(new AccountEmailRow { Email = email, AccountId = accountId });
        }

        private async Task DropIdentityTablesAsync(CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                DROP TABLE IF EXISTS "AspNetUserTokens";
                DROP TABLE IF EXISTS "AspNetUserLogins";
                DROP TABLE IF EXISTS "AspNetUserClaims";
                DROP TABLE IF EXISTS "AspNetRoleClaims";
                DROP TABLE IF EXISTS "AspNetUserRoles";
                DROP TABLE IF EXISTS "AspNetRoles";
                DROP TABLE IF EXISTS "AspNetUsers";
                """,
                cancellationToken);
        }

        private sealed class LegacyUser
        {
            public string Id { get; set; } = "";

            public string? Email { get; set; }

            public string? PasswordHash { get; set; }

            public string? RoleName { get; set; }
        }
    }
}
