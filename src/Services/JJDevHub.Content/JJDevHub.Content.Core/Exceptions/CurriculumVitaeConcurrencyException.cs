namespace JJDevHub.Content.Core.Exceptions;

public sealed class CurriculumVitaeConcurrencyException : ContentDomainException
{
    public CurriculumVitaeConcurrencyException()
        : base(
            "CONTENT.CURRICULUM_VITAE.CONCURRENCY_MISMATCH",
            "Curriculum vitae was modified by another process. Refresh and try again.")
    {
    }
}
