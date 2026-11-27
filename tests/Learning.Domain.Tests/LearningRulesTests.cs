using JJDevHub.Shared.Kernel;

using Learning.Domain;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Learning.Domain.Tests
{
    public sealed class LearningRulesTests
    {
        private static readonly DateTimeOffset At = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Publish_blank_title_returns_invalid()
        {
            Result<Technology> published = Technology.Publish(SlugOf("dotnet"), LocalizedText.Empty, SlugOf("languages"), Clock());

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            published.Error.Code.ShouldBe("learning.title.blank");
        }

        [Fact]
        public void ChangeSlug_after_publish_returns_frozen()
        {
            Technology technology = PublishedTechnology();

            Result changed = technology.ChangeSlug(SlugOf("csharp"));

            changed.IsSuccess.ShouldBeFalse();
            changed.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            changed.Error.Code.ShouldBe("learning.slug.frozen");
            technology.Slug.Value.ShouldBe("dotnet");
        }

        [Fact]
        public void Publish_sets_time_from_provider()
        {
            Result<Technology> published = Technology.Publish(SlugOf("dotnet"), TextOf(".NET", null), SlugOf("languages"), Clock());

            published.IsSuccess.ShouldBeTrue();
            published.Value.PublishedAt.ShouldBe(At);
            published.Value.Id.ShouldNotBe(Guid.Empty);
            published.Value.CategorySlug.Value.ShouldBe("languages");
            published.Value.Name.Polish.ShouldBe(".NET");
        }

        [Fact]
        public void Create_with_space_returns_invalid()
        {
            Result<Slug> slug = Slug.Create("dot net");

            slug.IsSuccess.ShouldBeFalse();
            slug.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            slug.Error.Code.ShouldBe("learning.slug.invalid");
        }

        [Fact]
        public void Same_text_is_equal()
        {
            LocalizedText left = TextOf("C#", "C#");
            LocalizedText right = TextOf("C#", "C#");

            left.Equals(right).ShouldBeTrue();
            left.GetHashCode().ShouldBe(right.GetHashCode());
            left.Equals(LocalizedText.Empty).ShouldBeFalse();
        }

        [Fact]
        public void Parse_unknown_returns_invalid()
        {
            Result<Visibility> parsed = VisibilityParser.Parse("hidden");

            parsed.IsSuccess.ShouldBeFalse();
            parsed.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            parsed.Error.Code.ShouldBe("learning.visibility.unknown");
        }

        [Fact]
        public void Publish_blank_body_returns_blank()
        {
            Result<Lesson> published = Lesson.Publish(SlugOf("intro"), TextOf("Wstęp", null), LocalizedText.Empty, SlugOf("basics"), Visibility.Public, Clock());

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            published.Error.Code.ShouldBe("learning.lesson.body_blank");
        }

        [Fact]
        public void ChangeName_keeps_slug()
        {
            Technology technology = PublishedTechnology();

            Result renamed = technology.ChangeName(TextOf("F#", null));

            renamed.IsSuccess.ShouldBeTrue();
            technology.Slug.Value.ShouldBe("dotnet");
            technology.Name.Polish.ShouldBe("F#");
        }

        [Fact]
        public void Publish_category_stores_name()
        {
            Result<Category> published = Category.Publish(SlugOf("languages"), TextOf("Języki", "Languages"), Clock());

            published.IsSuccess.ShouldBeTrue();
            published.Value.Slug.Value.ShouldBe("languages");
            published.Value.Name.English.ShouldBe("Languages");
            published.Value.PublishedAt.ShouldBe(At);
        }

        [Fact]
        public void Publish_course_stores_technology_slug()
        {
            Result<Course> published = Course.Publish(SlugOf("basics"), TextOf("Podstawy", null), SlugOf("dotnet"), Visibility.Students, Clock());

            published.IsSuccess.ShouldBeTrue();
            published.Value.TechnologySlug.Value.ShouldBe("dotnet");
            published.Value.Visibility.ShouldBe(Visibility.Students);
        }

        [Fact]
        public void Publish_lesson_stores_markdown_body()
        {
            Result<Lesson> published = Lesson.Publish(SlugOf("intro"), TextOf("Wstęp", null), TextOf("# Tytuł", "# Title"), SlugOf("basics"), Visibility.Private, Clock());

            published.IsSuccess.ShouldBeTrue();
            published.Value.Body.Polish.ShouldBe("# Tytuł");
            published.Value.Body.English.ShouldBe("# Title");
            published.Value.CourseSlug.Value.ShouldBe("basics");
            published.Value.Visibility.ShouldBe(Visibility.Private);
        }

        [Fact]
        public void Complete_again_keeps_completed_at()
        {
            StudentProgress progress = StudentProgress.Complete("student", SlugOf("intro"), At);

            StudentProgress again = progress.Complete(At.AddHours(2));

            again.ShouldBeSameAs(progress);
            again.CompletedAt.ShouldBe(At);
        }

        private static FakeTimeProvider Clock()
        {
            return new FakeTimeProvider(At);
        }

        private static Slug SlugOf(string value)
        {
            Result<Slug> slug = Slug.Create(value);
            slug.IsSuccess.ShouldBeTrue();
            return slug.Value;
        }

        private static LocalizedText TextOf(string? polish, string? english)
        {
            Result<LocalizedText> text = LocalizedText.Create(polish, english);
            text.IsSuccess.ShouldBeTrue();
            return text.Value;
        }

        private static Technology PublishedTechnology()
        {
            Result<Technology> published = Technology.Publish(SlugOf("dotnet"), TextOf("C#", "C#"), SlugOf("languages"), Clock());
            published.IsSuccess.ShouldBeTrue();
            return published.Value;
        }
    }
}