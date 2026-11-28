using Learning.Application.Ports;
using Learning.Domain;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfTechnologyRepository(LearningDbContext context) : ITechnologyRepository
    {
        public Task AddAsync(Technology technology, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Technologies.Add(technology);
            return Task.CompletedTask;
        }

        public Task<Technology?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            Slug? parsed = StoredSlug.Parse(slug);
            if (parsed is not Slug value)
            {
                return Task.FromResult<Technology?>(null);
            }

            return context.Technologies.SingleOrDefaultAsync(
                technology => technology.Slug == value,
                cancellationToken);
        }
    }
}