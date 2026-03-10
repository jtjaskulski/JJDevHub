namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public abstract class AuditableEntity : Entity
{
    public bool IsActive { get; protected set; } = true;
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; protected set; }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MarkModified() => ModifiedDate = DateTime.UtcNow;
}
