using JJDevHub.EventStore;
using JJDevHub.News.Domain.Articles;

namespace News.Application.Tests
{

    internal sealed class TestArticleStreamNaming : IStreamNaming<Article>
    {
        public string StreamId(Guid id) => $"article-{id}";

        public string? IdentityGuid(Article aggregate) => aggregate.AuthorIdentityGuid;
    }
}
