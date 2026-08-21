namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
