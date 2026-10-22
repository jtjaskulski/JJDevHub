using JJDevHub.Cqrs;

namespace JJDevHub.Api.Auth
{
    public sealed record RenameAccountCommand(Guid AccountId, string DisplayName, int ExpectedVersion)
        : ICommand<AccountProfile>;
}
