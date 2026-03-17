using JJDevHub.Content.Core.Entities;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Repositories;

public interface ICurriculumVitaeRepository : IRepository<CurriculumVitae>
{
    Task<CurriculumVitae?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CurriculumVitae curriculumVitae, CancellationToken cancellationToken = default);
    void Update(CurriculumVitae curriculumVitae);
}
