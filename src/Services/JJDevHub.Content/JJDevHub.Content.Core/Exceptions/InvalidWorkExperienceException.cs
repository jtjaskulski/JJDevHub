namespace JJDevHub.Content.Core.Exceptions;

public class InvalidWorkExperienceException : ContentDomainException
{
    public InvalidWorkExperienceException(string code, string message)
        : base(code, message)
    {
    }
}
