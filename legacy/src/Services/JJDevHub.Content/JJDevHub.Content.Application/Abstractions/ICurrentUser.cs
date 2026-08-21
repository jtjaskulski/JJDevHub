namespace JJDevHub.Content.Application.Abstractions;

/// <summary>
/// Zalogowany użytkownik w kontekście żądania (np. claim <c>sub</c> z JWT).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Subject (OIDC sub) lub null przy braku uwierzytelnienia.</summary>
    string? Subject { get; }
}
