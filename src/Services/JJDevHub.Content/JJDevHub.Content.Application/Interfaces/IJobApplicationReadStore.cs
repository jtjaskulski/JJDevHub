using JJDevHub.Content.Application.ReadModels;

namespace JJDevHub.Content.Application.Interfaces;

public interface IJobApplicationReadStore
{
    Task<JobApplicationReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobApplicationReadModel>> GetAllAsync(CancellationToken cancellationToken);

    Task UpsertAsync(JobApplicationReadModel model, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
