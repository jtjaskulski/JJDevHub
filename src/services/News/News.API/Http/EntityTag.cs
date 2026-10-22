namespace JJDevHub.News.Api.Http
{
    public static class EntityTag
    {
        public static string From(int version) => $"\"{version}\"";

        public static bool TryParse(string? header, out int version)
        {
            version = 0;
            if (string.IsNullOrEmpty(header) || header.Length < 3 || header[0] != '"' || header[^1] != '"')
            {
                return false;
            }

            if (!int.TryParse(header[1..^1], out version))
            {
                return false;
            }

            return header == From(version);
        }
    }
}
