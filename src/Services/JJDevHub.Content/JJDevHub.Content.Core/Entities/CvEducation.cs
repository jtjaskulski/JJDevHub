using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CvEducation : Entity
{
    public Guid CurriculumVitaeId { get; internal set; }

    public string Institution { get; private set; } = null!;
    public string FieldOfStudy { get; private set; } = null!;
    public EducationDegree Degree { get; private set; }
    public DateRange Period { get; private set; } = null!;

    private CvEducation() { }

    internal CvEducation(string institution, string fieldOfStudy, EducationDegree degree, DateRange period)
    {
        Institution = institution.Trim();
        FieldOfStudy = fieldOfStudy.Trim();
        Degree = degree;
        Period = period;
    }
}
