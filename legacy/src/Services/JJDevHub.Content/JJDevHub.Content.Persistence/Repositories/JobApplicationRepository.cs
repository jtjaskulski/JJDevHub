using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Persistence.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly ContentDbContext _context;

    public JobApplicationRepository(ContentDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.JobApplications
            .Include("_requirements")
            .Include("_notes")
            .Include("_interviewStages")
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.JobApplications
            .Include("_requirements")
            .Include("_notes")
            .Include("_interviewStages")
            .OrderByDescending(j => j.AppliedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobApplication application, CancellationToken cancellationToken)
    {
        await _context.JobApplications.AddAsync(application, cancellationToken);
    }

    public void Update(JobApplication application)
    {
        _context.JobApplications.Update(application);
    }

    public void Delete(JobApplication application)
    {
        application.MarkAsDeleted();
        _context.JobApplications.Update(application);
    }
}
