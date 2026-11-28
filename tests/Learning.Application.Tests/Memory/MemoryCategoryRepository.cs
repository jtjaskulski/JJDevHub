using Learning.Application.Ports;
using Learning.Domain;

namespace Learning.Application.Tests.Memory
{
    public sealed class MemoryCategoryRepository : ICategoryRepository
    {
        private readonly Dictionary<string, Category> _items = [];

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(category.Slug.Value, category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(slug, out Category? category);
            return Task.FromResult(category);
        }
    }
}