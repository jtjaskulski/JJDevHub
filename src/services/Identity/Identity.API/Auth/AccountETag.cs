namespace JJDevHub.Api.Auth
{
    internal static class AccountETag
    {
        public static string Format(int version) => $"\"{version}\"";

        public static bool TryRead(string? header, out int version)
        {
            version = 0;
            if (string.IsNullOrWhiteSpace(header))
            {
                return false;
            }

            var trimmed = header.Trim();
            if (trimmed.StartsWith("W/", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..].Trim();
            }

            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            {
                trimmed = trimmed[1..^1];
            }

            return int.TryParse(trimmed, out version);
        }
    }
}
