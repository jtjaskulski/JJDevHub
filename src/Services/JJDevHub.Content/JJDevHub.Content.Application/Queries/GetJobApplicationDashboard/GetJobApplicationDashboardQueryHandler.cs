using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplicationDashboard;

public class GetJobApplicationDashboardQueryHandler
    : IQueryHandler<GetJobApplicationDashboardQuery, JobApplicationDashboardDto>
{
    private readonly IJobApplicationReadStore _readStore;

    public GetJobApplicationDashboardQueryHandler(IJobApplicationReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<JobApplicationDashboardDto> Handle(
        GetJobApplicationDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _readStore.GetAllAsync(cancellationToken);
        var dict = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(s => s, _ => 0);

        foreach (var a in all)
            dict[a.Status]++;

        return new JobApplicationDashboardDto(all.Count, dict);
    }
}
