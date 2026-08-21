using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetWorkExperienceById;

public record GetWorkExperienceByIdQuery(Guid Id) : IQuery<WorkExperienceDto?>;
