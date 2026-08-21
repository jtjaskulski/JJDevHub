using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplications;

public record GetJobApplicationsQuery(
    ApplicationStatus? Status = null,
    string? CompanyContains = null) : IQuery<IReadOnlyList<JobApplicationDto>>;
