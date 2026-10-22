using System.Text.Json.Serialization;

namespace JJDevHub.Api.Auth
{
    internal sealed record Jwk(
        [property: JsonPropertyName("n")] string N,
        [property: JsonPropertyName("e")] string E,
        [property: JsonPropertyName("kid")] string Kid)
    {
        [JsonPropertyName("kty")]
        public string Kty => "RSA";

        [JsonPropertyName("use")]
        public string Use => "sig";

        [JsonPropertyName("alg")]
        public string Alg => "RS256";
    }
}
