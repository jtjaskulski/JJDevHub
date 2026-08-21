namespace JJDevHub.Api.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string Token, DateTime ExpiresAt);

public sealed record MeResponse(string Id, string? Email);
