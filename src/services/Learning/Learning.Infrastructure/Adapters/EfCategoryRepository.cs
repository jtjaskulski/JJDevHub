using Learning.Application.Ports;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfCategoryRepository(LearningDbContext context) : ICategoryRepository
    {
        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            Slug? parsed = StoredSlug.Parse(slug);
            if (parsed is not Slug value)
            {
                return Task.FromResult<Category?>(null);
            }

            return context.Categories.SingleOrDefaultAsync(category => category.Slug == value, cancellationToken);
        }
    }
}
