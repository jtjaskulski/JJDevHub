using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.Api.Tests
{
    public sealed class JwksTests : IClassFixture<ApiFactory>
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _client;

        public JwksTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Discovery_points_to_jwks()
        {
            var response = await _client.GetAsync("/.well-known/openid-configuration");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var document = await response.Content.ReadFromJsonAsync<JsonElement>();
            var uri = document.GetProperty("jwks_uri").GetString();
            Assert.EndsWith("/.well-known/jwks.json", uri);
        }

        [Fact]
        public async Task Jwks_exposes_signing_key()
        {
            var response = await _client.GetAsync("/.well-known/jwks.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var document = await response.Content.ReadFromJsonAsync<JsonElement>();
            var key = Assert.Single(document.GetProperty("keys").EnumerateArray());
            Assert.Equal("RSA", key.GetProperty("kty").GetString());
            Assert.False(string.IsNullOrWhiteSpace(key.GetProperty("kid").GetString()));
        }

        [Fact]
        public async Task Token_validates_with_jwks_key()
        {
            var registered = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"jwks-{Guid.NewGuid():N}@test.com", password = "Password1" });
            var body = await registered.Content.ReadFromJsonAsync<AuthResponse>(Json);
            var jwks = await _client.GetFromJsonAsync<JsonElement>("/.well-known/jwks.json");
            var jwk = jwks.GetProperty("keys").EnumerateArray().Single();
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("n").GetString()),
                Exponent = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("e").GetString()),
            });
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(body?.Token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "JJDevHub",
                ValidateAudience = true,
                ValidAudience = "JJDevHub",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = jwk.GetProperty("kid").GetString() },
            });

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task Token_uses_rs256()
        {
            var registered = await _client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"alg-{Guid.NewGuid():N}@test.com", password = "Password1" });
            var body = await registered.Content.ReadFromJsonAsync<AuthResponse>(Json);
            var jwt = new JsonWebTokenHandler().ReadJsonWebToken(body?.Token);

            Assert.Equal("RS256", jwt.Alg);
        }

        private sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt);
    }
}
