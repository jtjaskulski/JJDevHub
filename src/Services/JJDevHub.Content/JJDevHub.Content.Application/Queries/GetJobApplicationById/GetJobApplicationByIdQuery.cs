using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplicationById;

public record GetJobApplicationByIdQuery(Guid Id) : IQuery<JobApplicationDto?>;
