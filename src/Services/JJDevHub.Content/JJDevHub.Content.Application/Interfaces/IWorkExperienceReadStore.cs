using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.ReadModels;

namespace JJDevHub.Content.Application.Interfaces;

public interface IWorkExperienceReadStore
{
    Task<WorkExperienceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkExperienceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkExperienceDto>> GetPublicAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(WorkExperienceReadModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
