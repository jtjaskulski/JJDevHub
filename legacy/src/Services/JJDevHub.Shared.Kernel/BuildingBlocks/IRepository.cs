namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public interface IRepository<T> where T : Entity, IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
