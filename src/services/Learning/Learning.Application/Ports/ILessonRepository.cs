using Learning.Domain;

namespace Learning.Application.Ports
{
    public interface ILessonRepository
    {
        Task AddAsync(Lesson lesson, CancellationToken cancellationToken);

        Task<Lesson?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    }
}