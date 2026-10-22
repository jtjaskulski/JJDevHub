using JJDevHub.Api.Domain;
using JJDevHub.EventStore;

namespace JJDevHub.Api.Data
{
    public sealed class AccountStreamNaming : IStreamNaming<Account>
    {
        public string StreamId(Guid id) => $"account-{id}";

        public string? IdentityGuid(Account aggregate) => aggregate.Id.ToString();
    }
}
