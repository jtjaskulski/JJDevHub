using JJDevHub.Domain;
using JJDevHub.News.Domain.Articles;

namespace News.Domain.Tests;

public sealed class ArticleTitleTests
{
    [Fact]
    public void Blank_title_is_invalid()
    {
        var result = ArticleTitle.Create("   ");

        Assert.Equal(ResultError.Invalid, result.Error);
    }

    [Fact]
    public void Null_title_is_invalid()
    {
        var result = ArticleTitle.Create(null);

        Assert.Equal(ResultError.Invalid, result.Error);
    }

    [Fact]
    public void Too_long_title_is_invalid()
    {
        var result = ArticleTitle.Create(new string('a', 201));

        Assert.Equal(ResultError.Invalid, result.Error);
    }

    [Fact]
    public void Title_is_trimmed()
    {
        var result = ArticleTitle.Create("  Nowy wpis  ");

        Assert.True(result.TryGetValue(out var title));
        Assert.Equal("Nowy wpis", title.Value);
    }

    [Fact]
    public void Same_text_is_equal()
    {
        var left = ArticleTitle.Create("Nowy wpis");
        var right = ArticleTitle.Create("Nowy wpis");

        Assert.True(left.TryGetValue(out var leftTitle));
        Assert.True(right.TryGetValue(out var rightTitle));
        Assert.True(leftTitle == rightTitle);
    }
}
