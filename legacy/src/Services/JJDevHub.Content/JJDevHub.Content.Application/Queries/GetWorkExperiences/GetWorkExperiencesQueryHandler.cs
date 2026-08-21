using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetWorkExperiences;

public class GetWorkExperiencesQueryHandler
    : IQueryHandler<GetWorkExperiencesQuery, IReadOnlyList<WorkExperienceDto>>
{
    private readonly IWorkExperienceReadStore _readStore;

    public GetWorkExperiencesQueryHandler(IWorkExperienceReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<IReadOnlyList<WorkExperienceDto>> Handle(
        GetWorkExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        return request.PublicOnly
            ? await _readStore.GetPublicAsync(cancellationToken)
            : await _readStore.GetAllAsync(cancellationToken);
    }
}
