using Learning.Application.Ports;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfCourseRepository(LearningDbContext context) : ICourseRepository
    {
        public Task AddAsync(Course course, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Courses.Add(course);
            return Task.CompletedTask;
        }

        public Task<Course?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            Slug? parsed = StoredSlug.Parse(slug);
            if (parsed is not Slug value)
            {
                return Task.FromResult<Course?>(null);
            }

            return context.Courses.SingleOrDefaultAsync(course => course.Slug == value, cancellationToken);
        }

        public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return context.Courses.SingleOrDefaultAsync(course => course.Id == id, cancellationToken);
        }
    }
}