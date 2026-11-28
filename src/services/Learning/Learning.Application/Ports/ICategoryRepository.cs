using Learning.Domain;

namespace Learning.Application.Ports
{
    public interface ICategoryRepository
    {
        Task AddAsync(Category category, CancellationToken cancellationToken);

        Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    }
}