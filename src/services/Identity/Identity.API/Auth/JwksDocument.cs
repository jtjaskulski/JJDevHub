using System.Text.Json.Serialization;

namespace JJDevHub.Api.Auth
{
    internal sealed record JwksDocument([property: JsonPropertyName("keys")] Jwk[] Keys);
}
