using Learning.Domain;

namespace Learning.Application.Ports
{
    public interface ICourseRepository
    {
        Task AddAsync(Course course, CancellationToken cancellationToken);

        Task<Course?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

        Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}