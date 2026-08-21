using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetCurriculumVitaeById;

public record GetCurriculumVitaeByIdQuery(Guid Id) : IQuery<CurriculumVitaeDto?>;
