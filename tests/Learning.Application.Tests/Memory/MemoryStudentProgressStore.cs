using Learning.Application.Ports;
using Learning.Domain;

namespace Learning.Application.Tests.Memory
{
    public sealed class MemoryStudentProgressStore : IStudentProgressStore
    {
        private readonly Dictionary<(string StudentId, string LessonSlug), StudentProgress> _items = [];

        public int Count => _items.Count;

        public Task<StudentProgress?> GetAsync(string studentId, string lessonSlug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue((studentId, lessonSlug), out StudentProgress? progress);
            return Task.FromResult(progress);
        }

        public Task AddAsync(StudentProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add((progress.StudentId, progress.LessonSlug.Value), progress);
            return Task.CompletedTask;
        }
    }
}