namespace JJDevHub.News.Domain.Articles;

public readonly record struct ArticleId(Guid Value)
{
    public static ArticleId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
