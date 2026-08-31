using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace JJDevHub.Api.Tests;

public sealed class AuthAndHealthTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _client;

    public AuthAndHealthTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_login_me_roundtrip()
    {
        var email = $"owner-{Guid.NewGuid():N}@test.com";
        var body = new { email, password = "Password1" };

        var register = await _client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(registered?.Token));

        var login = await _client.PostAsJsonAsync("/api/auth/login", body);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(loggedIn?.Token));

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn!.Token);
        var me = await _client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<MeResponse>(Json);
        Assert.Equal(email, profile?.Email);
        Assert.False(string.IsNullOrWhiteSpace(profile?.Id));
    }

    private sealed record AuthResponse(string Token, DateTime ExpiresAt);

    private sealed record MeResponse(string Id, string? Email);
}
