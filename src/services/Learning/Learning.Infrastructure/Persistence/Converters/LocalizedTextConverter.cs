using System.Text.Json;

using Learning.Domain;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Learning.Infrastructure.Persistence
{
    public sealed class LocalizedTextConverter : ValueConverter<LocalizedText, string>
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public LocalizedTextConverter()
            : base(text => ToJson(text), json => FromJson(json))
        {
        }

        private static string ToJson(LocalizedText text)
        {
            return JsonSerializer.Serialize(new StoredText(text.Polish, text.English), JsonOptions);
        }

        private static LocalizedText FromJson(string json)
        {
            StoredText? stored = JsonSerializer.Deserialize<StoredText>(json, JsonOptions);
            if (stored is null)
            {
                throw new InvalidOperationException("learning.title.blank");
            }

            return LocalizedText.Restore(stored.Polish, stored.English);
        }

        private sealed record StoredText(string? Polish, string? English);
    }
}