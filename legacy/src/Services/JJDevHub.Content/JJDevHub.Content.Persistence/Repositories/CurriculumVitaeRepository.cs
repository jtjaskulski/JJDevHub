using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Persistence.Repositories;

public class CurriculumVitaeRepository : ICurriculumVitaeRepository
{
    private readonly ContentDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public CurriculumVitaeRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<CurriculumVitae?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.CurriculumVitaes
            .Include("_skills")
            .Include("_educations")
            .Include("_projects")
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CurriculumVitae>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.CurriculumVitaes
            .Include("_skills")
            .Include("_educations")
            .Include("_projects")
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CurriculumVitae curriculumVitae, CancellationToken cancellationToken)
    {
        await _context.CurriculumVitaes.AddAsync(curriculumVitae, cancellationToken);
    }

    public void Update(CurriculumVitae curriculumVitae)
    {
        _context.CurriculumVitaes.Update(curriculumVitae);
    }

    public void Delete(CurriculumVitae curriculumVitae)
    {
        curriculumVitae.MarkAsDeleted();
        _context.CurriculumVitaes.Update(curriculumVitae);
    }
}
