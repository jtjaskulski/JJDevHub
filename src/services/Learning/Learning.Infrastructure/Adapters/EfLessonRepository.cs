using Learning.Application.Ports;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfLessonRepository(LearningDbContext context) : ILessonRepository
    {
        public Task AddAsync(Lesson lesson, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Lessons.Add(lesson);
            return Task.CompletedTask;
        }

        public Task<Lesson?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            Slug? parsed = StoredSlug.Parse(slug);
            if (parsed is not Slug value)
            {
                return Task.FromResult<Lesson?>(null);
            }

            return context.Lessons.SingleOrDefaultAsync(lesson => lesson.Slug == value, cancellationToken);
        }
    }
}