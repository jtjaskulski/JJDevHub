using JJDevHub.Domain;

namespace JJDevHub.News.Domain.Articles;

public sealed class ArticleTitle : ValueObject
{
    public const int MaxLength = 200;

    private ArticleTitle(string value) => Value = value;

    public string Value { get; }

    public static Result<ArticleTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ArticleTitle>.Fail(ResultError.Invalid);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return Result<ArticleTitle>.Fail(ResultError.Invalid);
        }

        return Result<ArticleTitle>.Ok(new ArticleTitle(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
