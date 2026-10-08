namespace JJDevHub.Api.Auth;

public sealed record AuthResponse(string Token, DateTime ExpiresAt);
