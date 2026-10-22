using System.Text.Json.Serialization;

namespace JJDevHub.Api.Auth
{
    internal sealed record DiscoveryDocument(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);
}
