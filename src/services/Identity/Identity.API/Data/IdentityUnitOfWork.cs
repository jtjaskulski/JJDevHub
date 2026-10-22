using JJDevHub.Cqrs;

namespace JJDevHub.Api.Data
{
    public sealed class IdentityUnitOfWork(IdentityWriteContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction => context.Database.CurrentTransaction is not null;

        public async Task BeginAsync(CancellationToken cancellationToken)
        {
            await context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await context.Database.CommitTransactionAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            await context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
