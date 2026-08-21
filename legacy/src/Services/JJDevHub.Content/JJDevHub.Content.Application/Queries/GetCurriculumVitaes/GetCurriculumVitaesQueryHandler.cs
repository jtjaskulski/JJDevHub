using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetCurriculumVitaes;

public class GetCurriculumVitaesQueryHandler
    : IQueryHandler<GetCurriculumVitaesQuery, IReadOnlyList<CurriculumVitaeDto>>
{
    private readonly ICurriculumVitaeReadStore _readStore;

    public GetCurriculumVitaesQueryHandler(ICurriculumVitaeReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<IReadOnlyList<CurriculumVitaeDto>> Handle(
        GetCurriculumVitaesQuery request,
        CancellationToken cancellationToken) =>
        _readStore.GetAllAsync(cancellationToken);
}
