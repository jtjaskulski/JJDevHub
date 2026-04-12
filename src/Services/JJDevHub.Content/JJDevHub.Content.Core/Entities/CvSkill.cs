using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CvSkill : Entity
{
    public Guid CurriculumVitaeId { get; internal set; }

    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public SkillLevel Level { get; private set; }

    private CvSkill() { }

    internal CvSkill(string name, string category, SkillLevel level)
    {
        Name = name.Trim();
        Category = category.Trim();
        Level = level;
    }
}
