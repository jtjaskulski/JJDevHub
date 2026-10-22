using JJDevHub.Cqrs;

namespace JJDevHub.Api.Auth
{
    public sealed record RegisterAccountCommand(string Email, string Password, string? InviteCode)
        : ICommand<AuthSession>;
}
