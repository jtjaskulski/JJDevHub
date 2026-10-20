namespace JJDevHub.Cqrs
{

    public interface IUnitOfWork
    {
        bool HasActiveTransaction { get; }

        Task BeginAsync(CancellationToken cancellationToken);

        Task CommitAsync(CancellationToken cancellationToken);

        Task RollbackAsync(CancellationToken cancellationToken);
    }
}
