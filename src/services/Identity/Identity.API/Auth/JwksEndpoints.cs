using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.Api.Auth
{
    public static class JwksEndpoints
    {
        public static WebApplication MapJwks(this WebApplication app)
        {
            app.MapGet("/.well-known/openid-configuration", (HttpRequest request, IOptions<JwtOptions> options) =>
                Results.Json(new DiscoveryDocument(
                    options.Value.Issuer,
                    $"{BaseUrl(request, options.Value)}/.well-known/jwks.json")))
                .AllowAnonymous()
                .AddEndpointFilter(Cache);

            app.MapGet("/.well-known/jwks.json", (SigningKeyProvider keys) =>
                Results.Json(new JwksDocument(keys.PublicKeys.Select(ToJwk).ToArray())))
                .AllowAnonymous()
                .AddEndpointFilter(Cache);

            return app;
        }

        private static ValueTask<object?> Cache(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers.CacheControl = "public, max-age=300";
            return next(context);
        }

        private static string BaseUrl(HttpRequest request, JwtOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.PublicUrl))
            {
                return options.PublicUrl.TrimEnd('/');
            }

            return $"{request.Scheme}://{request.Host}";
        }

        private static Jwk ToJwk(RsaSecurityKey key)
        {
            var rsa = key.Rsa ?? throw new InvalidOperationException("Signing key has no RSA instance.");
            var parameters = rsa.ExportParameters(false);
            return new Jwk(
                Base64UrlEncoder.Encode(parameters.Modulus ?? []),
                Base64UrlEncoder.Encode(parameters.Exponent ?? []),
                key.KeyId ?? "");
        }
    }
}
