using JJDevHub.Shared.Kernel;

using Learning.Application.Ports;
using Learning.Application.Tests.Memory;
using Learning.Application.UseCases.Commands;
using Learning.Application.UseCases.Handlers;
using Learning.Domain;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Learning.Application.Tests
{
    public sealed class LearningApplicationTests
    {
        private static readonly DateTimeOffset At = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Publish_course_without_technology_returns_missing()
        {
            var technologies = new MemoryTechnologyRepository();
            var courses = new MemoryCourseRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new PublishCourseHandler(courses, technologies, unitOfWork, Clock());

            Result<Guid> published = await handler.HandleAsync(
                new PublishCourseCommand("basics", "dotnet", "Podstawy", "Basics", "public"),
                CancellationToken.None);

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            published.Error.Code.ShouldBe("learning.technology.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Publish_technology_without_category_returns_missing()
        {
            var categories = new MemoryCategoryRepository();
            var technologies = new MemoryTechnologyRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new PublishTechnologyHandler(technologies, categories, unitOfWork, Clock());

            Result<Guid> published = await handler.HandleAsync(
                new PublishTechnologyCommand("dotnet", "languages", "C#", "C#"),
                CancellationToken.None);

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            published.Error.Code.ShouldBe("learning.category.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Publish_technology_with_bad_slug_returns_invalid()
        {
            var categories = new MemoryCategoryRepository();
            var technologies = new MemoryTechnologyRepository();
            var unitOfWork = new RecordingUnitOfWork();
            await categories.AddAsync(PublishedCategory(), CancellationToken.None);
            var handler = new PublishTechnologyHandler(technologies, categories, unitOfWork, Clock());

            Result<Guid> published = await handler.HandleAsync(
                new PublishTechnologyCommand("dot net", "languages", "C#", "C#"),
                CancellationToken.None);

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            published.Error.Code.ShouldBe("learning.slug.invalid");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Publish_lesson_without_course_returns_missing()
        {
            var lessons = new MemoryLessonRepository();
            var courses = new MemoryCourseRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new PublishLessonHandler(lessons, courses, unitOfWork, Clock());

            Result<Guid> published = await handler.HandleAsync(
                new PublishLessonCommand("intro", "basics", "Wstęp", "Intro", "# Tytuł", "# Title", "public"),
                CancellationToken.None);

            published.IsSuccess.ShouldBeFalse();
            published.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            published.Error.Code.ShouldBe("learning.course.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Revise_unknown_lesson_returns_missing()
        {
            var lessons = new MemoryLessonRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new ReviseLessonHandler(lessons, unitOfWork);

            Result revised = await handler.HandleAsync(
                new ReviseLessonCommand("intro", "Nowa", "New", "# Nowe", "# New", null),
                CancellationToken.None);

            revised.IsSuccess.ShouldBeFalse();
            revised.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            revised.Error.Code.ShouldBe("learning.lesson.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Revise_slug_returns_frozen()
        {
            var lessons = new MemoryLessonRepository();
            var unitOfWork = new RecordingUnitOfWork();
            Lesson lesson = PublishedLesson();
            await lessons.AddAsync(lesson, CancellationToken.None);
            var handler = new ReviseLessonHandler(lessons, unitOfWork);

            Result revised = await handler.HandleAsync(
                new ReviseLessonCommand("intro", "Nowa", "New", "# Nowe", "# New", "inny"),
                CancellationToken.None);

            revised.IsSuccess.ShouldBeFalse();
            revised.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            revised.Error.Code.ShouldBe("learning.slug.frozen");
            lesson.Slug.Value.ShouldBe("intro");
            lesson.Name.Polish.ShouldBe("Wstęp");
            lesson.Body.Polish.ShouldBe("# Tytuł");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Revise_updates_title_and_markdown()
        {
            var lessons = new MemoryLessonRepository();
            var unitOfWork = new RecordingUnitOfWork();
            Lesson lesson = PublishedLesson();
            await lessons.AddAsync(lesson, CancellationToken.None);
            var handler = new ReviseLessonHandler(lessons, unitOfWork);

            Result revised = await handler.HandleAsync(
                new ReviseLessonCommand("intro", "Nowa", "New", "# Nowe", "# New", null),
                CancellationToken.None);

            revised.IsSuccess.ShouldBeTrue();
            lesson.Slug.Value.ShouldBe("intro");
            lesson.Name.Polish.ShouldBe("Nowa");
            lesson.Name.English.ShouldBe("New");
            lesson.Body.Polish.ShouldBe("# Nowe");
            lesson.Body.English.ShouldBe("# New");
            unitOfWork.Saves.ShouldBe(1);
        }

        [Fact]
        public async Task Complete_without_student_returns_missing()
        {
            var progress = new MemoryStudentProgressStore();
            var lessons = new MemoryLessonRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new CompleteLessonHandler(progress, lessons, unitOfWork, Clock());

            Result completed = await handler.HandleAsync(
                new CompleteLessonCommand("intro", "   "),
                CancellationToken.None);

            completed.IsSuccess.ShouldBeFalse();
            completed.Error.Kind.ShouldBe(ResultErrorKind.Invalid);
            completed.Error.Code.ShouldBe("learning.student.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Complete_unknown_lesson_returns_missing()
        {
            var progress = new MemoryStudentProgressStore();
            var lessons = new MemoryLessonRepository();
            var unitOfWork = new RecordingUnitOfWork();
            var handler = new CompleteLessonHandler(progress, lessons, unitOfWork, Clock());

            Result completed = await handler.HandleAsync(
                new CompleteLessonCommand("intro", "student"),
                CancellationToken.None);

            completed.IsSuccess.ShouldBeFalse();
            completed.Error.Kind.ShouldBe(ResultErrorKind.NotFound);
            completed.Error.Code.ShouldBe("learning.lesson.missing");
            unitOfWork.Saves.ShouldBe(0);
        }

        [Fact]
        public async Task Complete_twice_keeps_the_first_timestamp()
        {
            var lessons = new MemoryLessonRepository();
            var progress = new MemoryStudentProgressStore();
            var unitOfWork = new RecordingUnitOfWork();
            FakeTimeProvider clock = Clock();
            await lessons.AddAsync(PublishedLesson(), CancellationToken.None);
            var handler = new CompleteLessonHandler(progress, lessons, unitOfWork, clock);
            var command = new CompleteLessonCommand("intro", "student");

            Result first = await handler.HandleAsync(command, CancellationToken.None);
            clock.Advance(TimeSpan.FromHours(2));
            Result second = await handler.HandleAsync(command, CancellationToken.None);
            StudentProgress? stored = await progress.GetAsync("student", "intro", CancellationToken.None);

            first.IsSuccess.ShouldBeTrue();
            second.IsSuccess.ShouldBeTrue();
            progress.Count.ShouldBe(1);
            stored.ShouldNotBeNull();
            stored.CompletedAt.ShouldBe(At);
            unitOfWork.Saves.ShouldBe(2);
        }

        [Fact]
        public void AddLearningApplication_resolves_handlers()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICategoryRepository>(new MemoryCategoryRepository());
            services.AddSingleton<ITechnologyRepository>(new MemoryTechnologyRepository());
            services.AddSingleton<ICourseRepository>(new MemoryCourseRepository());
            services.AddSingleton<ILessonRepository>(new MemoryLessonRepository());
            services.AddSingleton<IStudentProgressStore>(new MemoryStudentProgressStore());
            services.AddSingleton<IUnitOfWork>(new RecordingUnitOfWork());
            services.AddLearningApplication();

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider scoped = scope.ServiceProvider;

            scoped.GetService<PublishTechnologyHandler>().ShouldNotBeNull();
            scoped.GetService<PublishCourseHandler>().ShouldNotBeNull();
            scoped.GetService<PublishLessonHandler>().ShouldNotBeNull();
            scoped.GetService<ReviseLessonHandler>().ShouldNotBeNull();
            scoped.GetService<CompleteLessonHandler>().ShouldNotBeNull();
            scoped.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
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

        private static Category PublishedCategory()
        {
            Result<Category> published = Category.Publish(SlugOf("languages"), TextOf("Języki", "Languages"), Clock());
            published.IsSuccess.ShouldBeTrue();
            return published.Value;
        }

        private static Lesson PublishedLesson()
        {
            Result<Lesson> published = Lesson.Publish(
                SlugOf("intro"),
                TextOf("Wstęp", "Intro"),
                TextOf("# Tytuł", "# Title"),
                SlugOf("basics"),
                Visibility.Public,
                Clock());
            published.IsSuccess.ShouldBeTrue();
            return published.Value;
        }
    }
}