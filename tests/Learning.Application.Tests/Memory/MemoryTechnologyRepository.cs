using Learning.Application.Ports;
using Learning.Domain;

namespace Learning.Application.Tests.Memory
{
    public sealed class MemoryTechnologyRepository : ITechnologyRepository
    {
        private readonly Dictionary<string, Technology> _items = [];

        public Task AddAsync(Technology technology, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(technology.Slug.Value, technology);
            return Task.CompletedTask;
        }

        public Task<Technology?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(slug, out Technology? technology);
            return Task.FromResult(technology);
        }
    }
}