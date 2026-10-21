using JJDevHub.Cqrs;

namespace JJDevHub.News.Infrastructure.Persistence
{
    public sealed class NewsUnitOfWork(NewsWriteContext context) : IUnitOfWork
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