using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Persistence.Repositories;

public class WorkExperienceRepository : IWorkExperienceRepository
{
    private readonly ContentDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public WorkExperienceRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<WorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.WorkExperiences.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<WorkExperience>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.WorkExperiences
            .OrderByDescending(e => e.Period.Start)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkExperience>> GetPublicAsync(CancellationToken cancellationToken)
    {
        return await _context.WorkExperiences
            .Where(e => e.IsPublic)
            .OrderByDescending(e => e.Period.Start)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkExperience workExperience, CancellationToken cancellationToken)
    {
        await _context.WorkExperiences.AddAsync(workExperience, cancellationToken);
    }

    public void Update(WorkExperience workExperience)
    {
        _context.WorkExperiences.Update(workExperience);
    }

    public void Delete(WorkExperience workExperience)
    {
        _context.WorkExperiences.Remove(workExperience);
    }
}
