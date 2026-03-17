namespace JJDevHub.Content.Core.Exceptions;

/// <summary>
/// Rekord został zmieniony przez innego użytkownika lub proces (wersja niezgodna z oczekiwaną).
/// </summary>
public sealed class WorkExperienceConcurrencyException : ContentDomainException
{
    public WorkExperienceConcurrencyException()
        : base(
            "CONTENT.WORK_EXPERIENCE.CONCURRENCY_MISMATCH",
            "Work experience was modified by another process. Refresh and try again.")
    {
    }
}
