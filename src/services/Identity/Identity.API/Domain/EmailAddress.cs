namespace JJDevHub.Api.Domain
{
    public static class EmailAddress
    {
        public static string? Normalize(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            string trimmed = email.Trim();
            int at = trimmed.IndexOf('@');
            if (at <= 0 || at == trimmed.Length - 1)
            {
                return null;
            }

            return trimmed.ToLowerInvariant();
        }
    }
}
