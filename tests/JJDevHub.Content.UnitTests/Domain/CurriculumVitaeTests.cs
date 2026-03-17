using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.ValueObjects;
using FluentAssertions;

namespace JJDevHub.Content.UnitTests.Domain;

public class CurriculumVitaeTests
{
    private static PersonalInfo SampleInfo() =>
        new("Jan", "Kowalski", "jan@example.com", phone: "+48111222333");

    [Fact]
    public void Create_ShouldEmitDomainEvent()
    {
        var cv = CurriculumVitae.Create(SampleInfo());

        cv.PersonalInfo.FirstName.Should().Be("Jan");
        cv.DomainEvents.Should().ContainSingle(e => e is CurriculumVitaeCreatedDomainEvent);
    }

    [Fact]
    public void AddSkill_ShouldAddAndEmitEvent()
    {
        var cv = CurriculumVitae.Create(SampleInfo());
        cv.ClearDomainEvents();

        cv.AddSkill("C#", "Backend", SkillLevel.Advanced);

        cv.Skills.Should().HaveCount(1);
        cv.Skills[0].Name.Should().Be("C#");
        cv.DomainEvents.Should().ContainSingle(e => e is CurriculumVitaeSkillAddedDomainEvent);
    }

    [Fact]
    public void AddSkill_EmptyName_ShouldThrow()
    {
        var cv = CurriculumVitae.Create(SampleInfo());

        var act = () => cv.AddSkill("  ", "X", SkillLevel.Beginner);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkWorkExperience_ShouldDeduplicate()
    {
        var cv = CurriculumVitae.Create(SampleInfo());
        var wid = Guid.NewGuid();

        cv.LinkWorkExperience(wid);
        cv.LinkWorkExperience(wid);

        cv.WorkExperienceIds.Should().HaveCount(1);
    }
}
