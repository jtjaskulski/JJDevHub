using Learning.Application.Ports;
using Learning.Domain;

namespace Learning.Application.Tests.Memory
{
    public sealed class MemoryLessonRepository : ILessonRepository
    {
        private readonly Dictionary<string, Lesson> _items = [];

        public Task AddAsync(Lesson lesson, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(lesson.Slug.Value, lesson);
            return Task.CompletedTask;
        }

        public Task<Lesson?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(slug, out Lesson? lesson);
            return Task.FromResult(lesson);
        }
    }
}