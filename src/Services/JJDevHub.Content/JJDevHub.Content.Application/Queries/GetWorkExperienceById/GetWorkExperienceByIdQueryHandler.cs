using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetWorkExperienceById;

public class GetWorkExperienceByIdQueryHandler
    : IQueryHandler<GetWorkExperienceByIdQuery, WorkExperienceDto?>
{
    private readonly IWorkExperienceReadStore _readStore;

    public GetWorkExperienceByIdQueryHandler(IWorkExperienceReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<WorkExperienceDto?> Handle(
        GetWorkExperienceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _readStore.GetByIdAsync(request.Id, cancellationToken);
    }
}
