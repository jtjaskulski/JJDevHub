namespace JJDevHub.Api.Auth
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "";

        public string Audience { get; set; } = "";

        public string? SigningKeyPath { get; set; }

        public string? PreviousSigningKeyPath { get; set; }

        public string? PublicUrl { get; set; }

        public int ExpiryMinutes { get; set; } = 60;
    }
}
