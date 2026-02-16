using JJDevHub.Shared.Kernel.Exceptions;

namespace JJDevHub.Content.Core.Exceptions;

public class ContentDomainException : DomainException
{
    public ContentDomainException(string message) : base(message)
    {
    }

    public ContentDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
