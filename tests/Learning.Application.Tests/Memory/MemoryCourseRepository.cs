using Learning.Application.Ports;
using Learning.Domain;

namespace Learning.Application.Tests.Memory
{
    public sealed class MemoryCourseRepository : ICourseRepository
    {
        private readonly Dictionary<string, Course> _items = [];

        public Task AddAsync(Course course, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(course.Slug.Value, course);
            return Task.CompletedTask;
        }

        public Task<Course?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(slug, out Course? course);
            return Task.FromResult(course);
        }

        public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (Course course in _items.Values)
            {
                if (course.Id == id)
                {
                    return Task.FromResult<Course?>(course);
                }
            }

            return Task.FromResult<Course?>(null);
        }
    }
}