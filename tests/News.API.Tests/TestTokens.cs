using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.News.Api.Tests
{
    public static class TestTokens
    {
        public static string For(Guid identityGuid, RSA signingKey)
        {
            var token = new JwtSecurityToken(
                issuer: "JJDevHub",
                audience: "JJDevHub",
                claims: [new Claim(JwtRegisteredClaimNames.Sub, identityGuid.ToString())],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
