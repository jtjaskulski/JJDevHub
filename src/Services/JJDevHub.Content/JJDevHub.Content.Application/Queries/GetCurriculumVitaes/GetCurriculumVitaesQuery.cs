using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetCurriculumVitaes;

public record GetCurriculumVitaesQuery : IQuery<IReadOnlyList<CurriculumVitaeDto>>;
