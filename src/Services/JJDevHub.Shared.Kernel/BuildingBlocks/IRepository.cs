namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public interface IRepository<T> where T : AggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
