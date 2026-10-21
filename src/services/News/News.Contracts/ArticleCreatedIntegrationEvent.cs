using System.Text.Json.Serialization;

using JJDevHub.Domain;
using JJDevHub.EventBus;

namespace JJDevHub.News.Contracts
{

    [EventName("news.integration.article-created.v1")]
    public sealed record ArticleCreatedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOn,
        Guid ArticleId,
        string Title,
        string? AuthorIdentityGuid) : IIntegrationEvent
    {
        [JsonIgnore]
        public string PartitionKey => ArticleId.ToString();
    }
}
