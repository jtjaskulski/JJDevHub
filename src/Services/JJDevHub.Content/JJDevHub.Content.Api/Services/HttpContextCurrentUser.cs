using System.Security.Claims;
using JJDevHub.Content.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace JJDevHub.Content.Api.Services;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpContextCurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    public string? Subject =>
        _http.HttpContext?.User.FindFirstValue("sub")
        ?? _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
