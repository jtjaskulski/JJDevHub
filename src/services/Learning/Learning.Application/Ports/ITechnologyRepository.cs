using Learning.Domain;

namespace Learning.Application.Ports
{
    public interface ITechnologyRepository
    {
        Task AddAsync(Technology technology, CancellationToken cancellationToken);

        Task<Technology?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    }
}