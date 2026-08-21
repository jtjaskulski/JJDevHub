namespace JJDevHub.Shared.Kernel.Exceptions;

public abstract class DomainException : Exception
{
    /// <summary>Stabilny kod pod i18n (np. CONTENT.WORK_EXPERIENCE.COMPANY_NAME_EMPTY).</summary>
    public string Code { get; }

    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    protected DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Komunikat jako treść techniczna; klient powinien preferować <see cref="Code"/>.</summary>
    protected DomainException(string message)
        : base(message)
    {
        Code = "DOMAIN.GENERIC";
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "DOMAIN.GENERIC";
    }
}
