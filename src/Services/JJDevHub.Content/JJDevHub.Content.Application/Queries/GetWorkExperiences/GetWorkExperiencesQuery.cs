using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetWorkExperiences;

public record GetWorkExperiencesQuery(bool PublicOnly = false) : IQuery<IReadOnlyList<WorkExperienceDto>>;
