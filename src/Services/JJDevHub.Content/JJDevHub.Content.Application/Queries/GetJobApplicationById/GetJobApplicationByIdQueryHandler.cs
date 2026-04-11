using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetJobApplicationById;

public class GetJobApplicationByIdQueryHandler : IQueryHandler<GetJobApplicationByIdQuery, JobApplicationDto?>
{
    private readonly IJobApplicationReadStore _readStore;

    public GetJobApplicationByIdQueryHandler(IJobApplicationReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<JobApplicationDto?> Handle(
        GetJobApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var m = await _readStore.GetByIdAsync(request.Id, cancellationToken);
        return m is null ? null : JobApplicationMapping.ToDto(m);
    }
}
