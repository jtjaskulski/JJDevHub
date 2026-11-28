using Learning.Application.Ports;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfStudentProgressStore(LearningDbContext context) : IStudentProgressStore
    {
        public Task<StudentProgress?> GetAsync(
            string studentId,
            string lessonSlug,
            CancellationToken cancellationToken)
        {
            Slug? parsed = StoredSlug.Parse(lessonSlug);
            if (parsed is not Slug value)
            {
                return Task.FromResult<StudentProgress?>(null);
            }

            return context.StudentProgress.SingleOrDefaultAsync(
                progress => progress.StudentId == studentId && progress.LessonSlug == value,
                cancellationToken);
        }

        public Task AddAsync(StudentProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.StudentProgress.Add(progress);
            return Task.CompletedTask;
        }
    }
}