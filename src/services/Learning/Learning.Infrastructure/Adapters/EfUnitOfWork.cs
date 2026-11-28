using Learning.Application;
using Learning.Application.Ports;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Learning.Infrastructure.Adapters
{
    public sealed class EfUnitOfWork(LearningDbContext context) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            AdvanceVersions();
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }
        }

        private void AdvanceVersions()
        {
            foreach (EntityEntry entry in context.ChangeTracker.Entries())
            {
                AdvanceVersion(entry);
            }
        }

        private static void AdvanceVersion(EntityEntry entry)
        {
            if (entry.State != EntityState.Modified)
            {
                return;
            }

            if (entry.Metadata.FindProperty("Version") is null)
            {
                return;
            }

            PropertyEntry version = entry.Property("Version");
            if (version.CurrentValue is not int current)
            {
                return;
            }

            version.CurrentValue = current + 1;
        }
    }
}