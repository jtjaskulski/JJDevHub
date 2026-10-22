using System.Security.Cryptography;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.Api.Auth
{
    public sealed class SigningKeyProvider
    {
        private readonly List<RsaSecurityKey> _publicKeys = [];

        public SigningKeyProvider(
            IOptions<JwtOptions> options,
            IHostEnvironment environment,
            ILogger<SigningKeyProvider> logger)
        {
            SigningKey = LoadSigningKey(options.Value, environment, logger);
            _publicKeys.Add(SigningKey);
            if (!string.IsNullOrWhiteSpace(options.Value.PreviousSigningKeyPath))
            {
                _publicKeys.Add(ReadKey(options.Value.PreviousSigningKeyPath));
            }
        }

        public RsaSecurityKey SigningKey { get; }

        public IReadOnlyList<RsaSecurityKey> PublicKeys => _publicKeys;

        private static RsaSecurityKey LoadSigningKey(
            JwtOptions options,
            IHostEnvironment environment,
            ILogger logger)
        {
            if (!string.IsNullOrWhiteSpace(options.SigningKeyPath) && File.Exists(options.SigningKeyPath))
            {
                return ReadKey(options.SigningKeyPath);
            }

            if (environment.IsDevelopment() || IsDocumentGeneration())
            {
                logger.LogWarning(
                    "Jwt signing key file is missing. Tokens are signed with an in-memory key that dies on restart.");
                return ToKey(RSA.Create(2048));
            }

            throw new InvalidOperationException("Jwt:SigningKeyPath must point at an RSA private key.");
        }

        private static bool IsDocumentGeneration()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.Contains("getdocument", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            string process = Environment.ProcessPath ?? string.Empty;
            if (process.Contains("getdocument", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string entry = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
            return entry.Contains("getdocument", StringComparison.OrdinalIgnoreCase);
        }

        private static RsaSecurityKey ReadKey(string path)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(path));
            return ToKey(rsa);
        }

        private static RsaSecurityKey ToKey(RSA rsa) => new(rsa) { KeyId = KeyId(rsa) };

        private static string KeyId(RSA rsa)
        {
            byte[] hash = SHA256.HashData(rsa.ExportSubjectPublicKeyInfo());
            return Base64UrlEncoder.Encode(hash)[..16];
        }
    }
}
