using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.Api.Auth
{
    public sealed class TokenService(IOptions<JwtOptions> jwtOptions, SigningKeyProvider signingKeys, TimeProvider time)
    {
        private readonly JwtOptions _jwt = jwtOptions.Value;

        public string CreateToken(Guid accountId, string email, IReadOnlyList<string> roles)
        {
            var credentials = new SigningCredentials(signingKeys.SigningKey, SecurityAlgorithms.RsaSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: time.GetUtcNow().UtcDateTime,
                expires: ExpiresAt().UtcDateTime,
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTimeOffset ExpiresAt() => time.GetUtcNow().AddMinutes(_jwt.ExpiryMinutes);
    }
}
