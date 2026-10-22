using JJDevHub.Cqrs;

namespace JJDevHub.Api.Auth
{
    public sealed record LoginAccountCommand(string Email, string Password) : IRequest<AuthSession>;
}
