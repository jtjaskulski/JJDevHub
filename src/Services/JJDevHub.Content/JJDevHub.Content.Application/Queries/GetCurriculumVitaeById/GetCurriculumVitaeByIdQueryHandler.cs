using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Queries.GetCurriculumVitaeById;

public class GetCurriculumVitaeByIdQueryHandler
    : IQueryHandler<GetCurriculumVitaeByIdQuery, CurriculumVitaeDto?>
{
    private readonly ICurriculumVitaeReadStore _readStore;

    public GetCurriculumVitaeByIdQueryHandler(ICurriculumVitaeReadStore readStore)
    {
        _readStore = readStore;
    }

    public Task<CurriculumVitaeDto?> Handle(
        GetCurriculumVitaeByIdQuery request,
        CancellationToken cancellationToken) =>
        _readStore.GetByIdAsync(request.Id, cancellationToken);
}
