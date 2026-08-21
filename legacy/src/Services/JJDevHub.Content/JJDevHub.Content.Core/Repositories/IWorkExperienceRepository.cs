using JJDevHub.Content.Core.Entities;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Repositories;

public interface IWorkExperienceRepository : IRepository<WorkExperience>
{
    Task<WorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkExperience>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkExperience>> GetPublicAsync(CancellationToken cancellationToken = default);
    Task AddAsync(WorkExperience workExperience, CancellationToken cancellationToken = default);
    void Update(WorkExperience workExperience);
    void Delete(WorkExperience workExperience);
}
