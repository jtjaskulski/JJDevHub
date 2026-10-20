using JJDevHub.EventStore;
using JJDevHub.News.Domain.Articles;

namespace JJDevHub.News.Infrastructure.Persistence
{

    public sealed class ArticleStreamNaming : IStreamNaming<Article>
    {
        public string StreamId(Guid id) => $"article-{id}";

        public string? IdentityGuid(Article aggregate) => aggregate.AuthorIdentityGuid;
    }
}
