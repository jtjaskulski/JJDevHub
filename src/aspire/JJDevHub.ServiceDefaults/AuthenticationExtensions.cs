using System.Security.Cryptography;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.ServiceDefaults
{
    public static class AuthenticationExtensions
    {
        public static TBuilder AddDefaultAuthentication<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            var section = builder.Configuration.GetSection("Identity");
            string? url = section["Url"];
            string? pem = section["SigningKeyPem"];
            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(pem))
            {
                return builder;
            }

            string? audience = section["Audience"];
            string? issuer = section["Issuer"];
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => ConfigureBearer(options, builder.Environment.IsDevelopment(), url, pem, audience, issuer));
            builder.Services.AddRolePolicies();
            return builder;
        }

        public static AuthorizationBuilder AddRolePolicies(this IServiceCollection services) =>
            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.Owner, policy => policy.RequireRole(Roles.Owner))
                .AddPolicy(Policies.Student, policy => policy.RequireRole(Roles.Student, Roles.Owner));

        private static void ConfigureBearer(
            JwtBearerOptions options,
            bool development,
            string? url,
            string? pem,
            string? audience,
            string? issuer)
        {
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = !development;
            options.Audience = audience;
            options.TokenValidationParameters.NameClaimType = "sub";
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(audience);
            options.TokenValidationParameters.ValidAudience = audience;
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
            if (!string.IsNullOrWhiteSpace(pem))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(rsa);
                options.TokenValidationParameters.ValidateIssuer = !string.IsNullOrWhiteSpace(issuer);
                options.TokenValidationParameters.ValidIssuer = issuer;
                return;
            }

            options.Authority = url;
        }
    }
}
