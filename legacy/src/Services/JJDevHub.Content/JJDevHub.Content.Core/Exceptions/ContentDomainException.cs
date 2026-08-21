using JJDevHub.Shared.Kernel.Exceptions;

namespace JJDevHub.Content.Core.Exceptions;

public class ContentDomainException : DomainException
{
    public ContentDomainException(string code, string message)
        : base(code, message)
    {
    }

    public ContentDomainException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }

    public ContentDomainException(string message)
        : base(message)
    {
    }

    public ContentDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
