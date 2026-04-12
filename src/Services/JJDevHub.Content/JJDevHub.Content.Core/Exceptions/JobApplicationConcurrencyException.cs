namespace JJDevHub.Content.Core.Exceptions;

public sealed class JobApplicationConcurrencyException : ContentDomainException
{
    public JobApplicationConcurrencyException()
        : base("CONTENT.JOB_APPLICATION.CONCURRENCY", "The job application was modified by another process.")
    {
    }
}
