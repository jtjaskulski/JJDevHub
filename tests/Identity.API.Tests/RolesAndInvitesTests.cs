using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using JJDevHub.Api.Auth;
using JJDevHub.Api.Data;
using JJDevHub.Api.Domain;
using JJDevHub.EventStore;
using JJDevHub.EventStore.Postgres;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Api.Tests
{
    public sealed class RolesAndInvitesTests : IClassFixture<ApiFactory>
    {
        private const string OwnerEmail = "owner@test.local";

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ApiFactory _factory;

        private readonly HttpClient _client;

        public RolesAndInvitesTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Owner_email_gets_owner_role()
        {
            var token = await OwnerTokenAsync();

            Assert.Contains("owner", Roles(token));
        }

        [Fact]
        public async Task Seeder_grants_owner_to_existing_account()
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var accounts = scope.ServiceProvider.GetRequiredService<IRepository<Account>>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
            var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var row = await write.AccountEmails.FindAsync([OwnerEmail]);
            Account account;
            if (row is null)
            {
                account = Account.Register(OwnerEmail, "Password1", hasher, time);
                await accounts.SaveAsync(account, CancellationToken.None);
                write.AccountEmails.Add(new AccountEmailRow { Email = account.Email, AccountId = account.Id });
                await write.SaveChangesAsync();
            }
            else
            {
                var loaded = await accounts.GetAsync(row.AccountId, CancellationToken.None);
                account = loaded.Value ?? throw new InvalidOperationException("Owner account is missing.");
                if (account.Roles.Contains("owner"))
                {
                    account.RevokeRole("owner", time);
                    await accounts.SaveAsync(account, CancellationToken.None);
                }
            }

            var seeder = scope.ServiceProvider.GetRequiredService<OwnerSeeder>();
            await seeder.StartAsync(CancellationToken.None);

            var granted = await write.Set<EventRecord>()
                .Where(record => record.StreamId == $"account-{account.Id}" && record.EventType == "identity.role-granted.v1")
                .ToListAsync();
            Assert.Contains(granted, record => record.Payload.Contains("owner", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Plain_registration_has_no_role()
        {
            var token = await RegisterAsync($"plain-{Guid.NewGuid():N}@test.com", null);

            Assert.Empty(Roles(token));
        }

        [Fact]
        public async Task Invite_registration_gets_student_role()
        {
            var code = await CreateInviteAsync(2, DateTimeOffset.UtcNow.AddDays(1));
            var token = await RegisterAsync($"student-{Guid.NewGuid():N}@test.com", code);
            var accountId = AccountId(token);

            Assert.Contains("student", Roles(token));
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var redeemed = await write.Set<EventRecord>()
                .Where(record => record.EventType == "identity.invite-redeemed.v1")
                .ToListAsync();
            Assert.Equal(1, redeemed.Count(record => record.Payload.Contains(accountId.ToString(), StringComparison.Ordinal)));
        }

        [Fact]
        public async Task Invite_code_is_case_and_dash_insensitive()
        {
            var code = await CreateInviteAsync(1, DateTimeOffset.UtcNow.AddDays(1));
            var dashed = $"{code[..4]}-{code[4..8]}-{code[8..]}".ToLowerInvariant();

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"dash-{Guid.NewGuid():N}@test.com", password = "Password1", inviteCode = dashed });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Expired_invite_returns_400_without_account()
        {
            _factory.Time.UtcNow = DateTimeOffset.UtcNow;
            var code = await CreateInviteAsync(2, _factory.Time.UtcNow.AddMinutes(-5));
            var email = $"expired-{Guid.NewGuid():N}@test.com";

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "Password1", inviteCode = code });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadAsStringAsync();
            Assert.Contains("inviteCode", problem, StringComparison.OrdinalIgnoreCase);
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            Assert.False(await write.AccountEmails.AnyAsync(row => row.Email == email));
        }

        [Fact]
        public async Task Exhausted_invite_returns_400()
        {
            var code = await CreateInviteAsync(1, DateTimeOffset.UtcNow.AddDays(1));
            await RegisterAsync($"used-{Guid.NewGuid():N}@test.com", code);

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"again-{Guid.NewGuid():N}@test.com", password = "Password1", inviteCode = code });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Invite_is_stored_as_hash()
        {
            var owner = await OwnerTokenAsync();
            using var request = Authorized(HttpMethod.Post, "/api/auth/invites", owner);
            request.Content = JsonContent.Create(new
            {
                label = "group",
                expiresOn = DateTimeOffset.UtcNow.AddDays(1),
                maxUses = 3,
            });
            var response = await _client.SendAsync(request);
            var created = await response.Content.ReadFromJsonAsync<CreatedInvite>(Json);

            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var stored = await write.Set<EventRecord>()
                .Where(record => record.EventType == "identity.invite-created.v1")
                .OrderByDescending(record => record.Position)
                .FirstAsync();
            Assert.NotNull(created);
            using var payload = JsonDocument.Parse(stored.Payload);
            var storedHash = payload.RootElement.GetProperty("codeHash").GetString();
            Assert.Equal(JJDevHub.Api.Invites.InviteCode.Hash(created.Code), storedHash);
            Assert.NotEqual(created.Code, storedHash);
        }

        [Fact]
        public async Task Student_cannot_create_invite()
        {
            var code = await CreateInviteAsync(2, DateTimeOffset.UtcNow.AddDays(1));
            var token = await RegisterAsync($"blocked-{Guid.NewGuid():N}@test.com", code);
            using var request = Authorized(HttpMethod.Post, "/api/auth/invites", token);
            request.Content = JsonContent.Create(new
            {
                label = "nope",
                expiresOn = DateTimeOffset.UtcNow.AddDays(1),
                maxUses = 1,
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Owner_grants_student_role()
        {
            var email = $"granted-{Guid.NewGuid():N}@test.com";
            var plain = await RegisterAsync(email, null);
            var accountId = AccountId(plain);
            var owner = await OwnerTokenAsync();
            using var grant = Authorized(HttpMethod.Put, $"/api/auth/users/{accountId}/roles/student", owner);
            var granted = await _client.SendAsync(grant);
            Assert.Equal(HttpStatusCode.NoContent, granted.StatusCode);

            var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
            var body = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
            Assert.NotNull(body);
            Assert.Contains("student", Roles(body.Token));
        }

        private async Task<string> OwnerTokenAsync()
        {
            var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = OwnerEmail, password = "Password1" });
            if (login.StatusCode == HttpStatusCode.OK)
            {
                var loggedIn = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
                return loggedIn?.Token ?? throw new InvalidOperationException("Owner login returned no token.");
            }

            return await RegisterAsync(OwnerEmail, null);
        }

        private async Task<string> RegisterAsync(string email, string? inviteCode)
        {
            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "Password1", inviteCode });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
            return body?.Token ?? throw new InvalidOperationException("Register returned no token.");
        }

        private async Task<string> CreateInviteAsync(int maxUses, DateTimeOffset expiresOn)
        {
            var owner = await OwnerTokenAsync();
            using var request = Authorized(HttpMethod.Post, "/api/auth/invites", owner);
            request.Content = JsonContent.Create(new { label = "group", expiresOn, maxUses });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CreatedInvite>(Json);
            return created?.Code ?? throw new InvalidOperationException("Invite returned no code.");
        }

        private static HttpRequestMessage Authorized(HttpMethod method, string path, string token)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private static Guid AccountId(string token) =>
            Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);

        private static IReadOnlyList<string> Roles(string token) =>
            new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
                .Where(claim => claim.Type == "role")
                .Select(claim => claim.Value)
                .ToArray();

        private sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt);

        private sealed record CreatedInvite(string Code);
    }
}
