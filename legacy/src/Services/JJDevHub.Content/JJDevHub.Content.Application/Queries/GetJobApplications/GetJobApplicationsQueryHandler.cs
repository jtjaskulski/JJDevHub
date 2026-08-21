using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplications;

public class GetJobApplicationsQueryHandler : IQueryHandler<GetJobApplicationsQuery, IReadOnlyList<JobApplicationDto>>
{
    private readonly IJobApplicationReadStore _readStore;

    public GetJobApplicationsQueryHandler(IJobApplicationReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<IReadOnlyList<JobApplicationDto>> Handle(
        GetJobApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _readStore.GetAllAsync(cancellationToken);
        IEnumerable<JobApplicationReadModel> q = all;

        if (request.Status.HasValue)
            q = q.Where(a => a.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.CompanyContains))
        {
            var needle = request.CompanyContains.Trim();
            q = q.Where(a =>
                a.CompanyName.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return q.Select(JobApplicationMapping.ToDto).ToList();
    }
}
