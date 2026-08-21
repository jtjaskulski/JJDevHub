using JJDevHub.Content.Core.Entities;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Repositories;

public interface IJobApplicationRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobApplication>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(JobApplication application, CancellationToken cancellationToken);

    void Update(JobApplication application);

    void Delete(JobApplication application);
}
