using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using JJDevHub.Api.Data;
using JJDevHub.EventStore.Postgres;
using JJDevHub.IntegrationEventLog;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Api.Tests
{
    public sealed class RegistrationOutboxTests : IClassFixture<ApiFactory>
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ApiFactory _factory;

        private readonly HttpClient _client;

        public RegistrationOutboxTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_creates_account_stream()
        {
            var (email, password, token) = await RegisterAsync();
            var accountId = AccountId(token);

            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var emailRow = await write.AccountEmails.SingleAsync(row => row.Email == email);
            Assert.Equal(accountId, emailRow.AccountId);
            var registered = await write.Set<EventRecord>()
                .SingleAsync(record => record.StreamId == $"account-{accountId}" && record.EventType == "identity.account-registered.v1");
            Assert.Contains("passwordHash", registered.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("displayName", registered.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(password, registered.Payload);
        }

        [Fact]
        public async Task Me_returns_name_from_stream()
        {
            var (email, _, token) = await RegisterAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var profile = await response.Content.ReadFromJsonAsync<MeResponse>(Json);
            Assert.Equal(email, profile?.Email);
            Assert.False(string.IsNullOrWhiteSpace(profile?.DisplayName));
        }

        [Fact]
        public async Task Rename_appends_fact()
        {
            var (_, _, token) = await RegisterAsync();
            var etag = await ETagAsync(token);

            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("If-Match", etag);
            request.Content = JsonContent.Create(new { displayName = "Ada Lovelace" });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var accountId = AccountId(token);
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var renamed = await write.Set<EventRecord>().AnyAsync(record =>
                record.StreamId == $"account-{accountId}" && record.EventType == "identity.account-renamed.v1");
            Assert.True(renamed);
        }

        [Fact]
        public async Task Rename_too_short_returns_400()
        {
            var (_, _, token) = await RegisterAsync();
            var etag = await ETagAsync(token);

            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("If-Match", etag);
            request.Content = JsonContent.Create(new { displayName = "x" });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_writes_outbox_row()
        {
            var (_, _, token) = await RegisterAsync();

            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var rows = await write.Set<OutboxMessage>()
                .Where(row => row.PartitionKey == AccountId(token).ToString())
                .ToListAsync();
            var row = Assert.Single(rows);
            Assert.Equal("identity.integration.user-registered.v1", row.Name);
        }

        [Fact]
        public async Task Outbox_payload_has_no_password()
        {
            var (_, password, token) = await RegisterAsync();

            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var row = await write.Set<OutboxMessage>().SingleAsync(item => item.PartitionKey == AccountId(token).ToString());
            Assert.DoesNotContain(password, row.Payload);
            Assert.DoesNotContain("passwordHash", row.Payload, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Duplicate_email_writes_no_outbox_row()
        {
            var (email, password, token) = await RegisterAsync();
            var accountId = AccountId(token).ToString();

            var duplicate = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var count = await write.Set<OutboxMessage>().CountAsync(row => row.PartitionKey == accountId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Processor_publishes_registration()
        {
            var (_, _, token) = await RegisterAsync();
            var accountId = AccountId(token).ToString();

            await using var scope = _factory.Services.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<IdentityWriteContext>>();
            await processor.RunOnceAsync(CancellationToken.None);
            var bus = _factory.Services.GetRequiredService<RecordingEventBus>();
            Assert.Contains(bus.Envelopes, envelope => envelope.PartitionKey == accountId);
        }

        [Fact]
        public async Task Failed_login_appends_event()
        {
            var (email, _, token) = await RegisterAsync();

            var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var failed = await write.Set<EventRecord>().AnyAsync(record =>
                record.StreamId == $"account-{AccountId(token)}" && record.EventType == "identity.login-failed.v1");
            Assert.True(failed);
        }

        [Fact]
        public async Task Fifth_failure_locks_account()
        {
            var (email, password, token) = await RegisterAsync();
            for (var attempt = 0; attempt < 4; attempt++)
            {
                await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
            }

            var fifth = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
            var duringLock = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

            Assert.Equal(HttpStatusCode.Unauthorized, fifth.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, duringLock.StatusCode);
            await using var scope = _factory.Services.CreateAsyncScope();
            var write = scope.ServiceProvider.GetRequiredService<IdentityWriteContext>();
            var locked = await write.Set<EventRecord>()
                .Where(record => record.StreamId == $"account-{AccountId(token)}" && record.EventType == "identity.login-failed.v1")
                .OrderBy(record => record.Version)
                .LastAsync();
            Assert.Contains("lockedUntil", locked.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"lockedUntil\":null", locked.Payload, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(string Email, string Password, string Token)> RegisterAsync()
        {
            var email = $"user-{Guid.NewGuid():N}@test.com";
            const string password = "Password1";
            var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
            if (body is null || string.IsNullOrWhiteSpace(body.Token))
            {
                throw new InvalidOperationException("Register did not return a token.");
            }

            return (email, password, body.Token);
        }

        private async Task<string> ETagAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tag = response.Headers.ETag?.Tag;
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new InvalidOperationException("Me did not return an ETag.");
            }

            return tag;
        }

        private static Guid AccountId(string token)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return Guid.Parse(jwt.Subject);
        }

        private sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt);

        private sealed record MeResponse(Guid IdentityGuid, string Email, string DisplayName, string[] Roles);
    }
}
