using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.ReadModels;

namespace JJDevHub.Content.Application.Interfaces;

public interface ICurriculumVitaeReadStore
{
    Task<CurriculumVitaeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurriculumVitaeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(CurriculumVitaeReadModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
