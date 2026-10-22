using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.EventStore;
using JJDevHub.ServiceDefaults;

namespace JJDevHub.Api.Auth
{
    public sealed class OwnerSeeder(IServiceScopeFactory scopes, IConfiguration configuration) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var email = EmailAddress.Normalize(configuration["Identity:OwnerEmail"]);
            if (email is null)
            {
                return;
            }

            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var accounts = scope.ServiceProvider.GetRequiredService<IRepository<Account>>();
            var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var row = await context.AccountEmails.FindAsync([email], cancellationToken);
            if (row is null)
            {
                return;
            }

            var loaded = await accounts.GetAsync(row.AccountId, cancellationToken);
            if (!loaded.TryGetValue(out var account) || account.Roles.Contains(Roles.Owner))
            {
                return;
            }

            account.GrantRole(Roles.Owner, time);
            await accounts.SaveAsync(account, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
