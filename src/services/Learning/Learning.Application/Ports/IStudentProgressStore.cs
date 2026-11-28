using Learning.Domain;

namespace Learning.Application.Ports
{
    public interface IStudentProgressStore
    {
        Task<StudentProgress?> GetAsync(string studentId, string lessonSlug, CancellationToken cancellationToken);

        Task AddAsync(StudentProgress progress, CancellationToken cancellationToken);
    }
}