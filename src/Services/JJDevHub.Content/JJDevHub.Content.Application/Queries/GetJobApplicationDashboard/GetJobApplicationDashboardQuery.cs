using JJDevHub.Content.Application.DTOs;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplicationDashboard;

public record GetJobApplicationDashboardQuery : IQuery<JobApplicationDashboardDto>;
