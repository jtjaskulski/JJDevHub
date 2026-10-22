namespace JJDevHub.Api.Auth
{
    public sealed record AuthSession(string Token, DateTimeOffset ExpiresAt);
}
