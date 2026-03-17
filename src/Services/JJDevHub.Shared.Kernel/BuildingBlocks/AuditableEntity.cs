namespace JJDevHub.Shared.Kernel.BuildingBlocks;

public abstract class AuditableEntity : Entity
{
    public bool IsActive { get; protected set; } = true;
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; protected set; }

    /// <summary>Subject (np. Keycloak sub) użytkownika, który utworzył rekord.</summary>
    public string? CreatedById { get; protected set; }

    /// <summary>Subject użytkownika przy ostatniej modyfikacji.</summary>
    public string? ModifiedById { get; protected set; }

    /// <summary>Wersja wiersza do optymistycznej współbieżności (inkrementowana przy zapisie).</summary>
    public long Version { get; protected set; }

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

    /// <summary>Wywoływane z warstwy persystencji przy INSERT.</summary>
    internal void ApplyPersistenceOnCreate(DateTime utcNow, string? userSubject)
    {
        CreatedDate = utcNow;
        CreatedById = userSubject;
        Version = 1;
    }

    /// <summary>Wywoływane z warstwy persystencji przy UPDATE.</summary>
    internal void ApplyPersistenceOnModify(DateTime utcNow, string? userSubject)
    {
        ModifiedDate = utcNow;
        ModifiedById = userSubject;
        Version++;
    }
}
