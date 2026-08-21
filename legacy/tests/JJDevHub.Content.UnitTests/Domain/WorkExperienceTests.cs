using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Events;
using JJDevHub.Content.Core.Exceptions;

namespace JJDevHub.Content.UnitTests.Domain;

public class WorkExperienceTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateWorkExperience()
    {
        var experience = WorkExperience.Create(
            "Microsoft", "Senior Developer",
            new DateTime(2023, 1, 1), null, true);

        experience.CompanyName.Should().Be("Microsoft");
        experience.Position.Should().Be("Senior Developer");
        experience.Period.Start.Should().Be(new DateTime(2023, 1, 1));
        experience.Period.End.Should().BeNull();
        experience.IsPublic.Should().BeTrue();
        experience.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldRaiseWorkExperienceCreatedDomainEvent()
    {
        var experience = WorkExperience.Create(
            "Google", "Engineer",
            new DateTime(2022, 6, 1), null, true);

        experience.DomainEvents.Should().ContainSingle();
        var domainEvent = experience.DomainEvents.First();
        domainEvent.Should().BeOfType<WorkExperienceCreatedDomainEvent>();

        var createdEvent = (WorkExperienceCreatedDomainEvent)domainEvent;
        createdEvent.WorkExperienceId.Should().Be(experience.Id);
        createdEvent.CompanyName.Should().Be("Google");
        createdEvent.Position.Should().Be("Engineer");
        createdEvent.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        var experience = WorkExperience.Create(
            "  Microsoft  ", "  Developer  ",
            new DateTime(2023, 1, 1), null, false);

        experience.CompanyName.Should().Be("Microsoft");
        experience.Position.Should().Be("Developer");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCompanyName_ShouldThrowDomainException(string? companyName)
    {
        var act = () => WorkExperience.Create(
            companyName!, "Developer",
            new DateTime(2023, 1, 1), null, true);

        act.Should().Throw<InvalidWorkExperienceException>()
            .WithMessage("*Company name*empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPosition_ShouldThrowDomainException(string? position)
    {
        var act = () => WorkExperience.Create(
            "Microsoft", position!,
            new DateTime(2023, 1, 1), null, true);

        act.Should().Throw<InvalidWorkExperienceException>()
            .WithMessage("*Position*empty*");
    }

    [Fact]
    public void Create_WithCompanyNameExceeding200Chars_ShouldThrowDomainException()
    {
        var longName = new string('A', 201);

        var act = () => WorkExperience.Create(
            longName, "Developer",
            new DateTime(2023, 1, 1), null, true);

        act.Should().Throw<InvalidWorkExperienceException>()
            .WithMessage("*Company name*200*");
    }

    [Fact]
    public void Update_ShouldModifyPropertiesAndRaiseEvent()
    {
        var experience = WorkExperience.Create(
            "OldCorp", "Junior",
            new DateTime(2020, 1, 1), null, false);
        experience.ClearDomainEvents();

        experience.Update("NewCorp", "Senior",
            new DateTime(2021, 1, 1), new DateTime(2023, 12, 31), true);

        experience.CompanyName.Should().Be("NewCorp");
        experience.Position.Should().Be("Senior");
        experience.IsPublic.Should().BeTrue();
        experience.Period.End.Should().Be(new DateTime(2023, 12, 31));

        experience.DomainEvents.Should().ContainSingle();
        experience.DomainEvents.First().Should().BeOfType<WorkExperienceUpdatedDomainEvent>();
    }

    [Fact]
    public void MarkAsDeleted_ShouldRaiseDeletedDomainEvent()
    {
        var experience = WorkExperience.Create(
            "Corp", "Dev", new DateTime(2023, 1, 1), null, true);
        experience.ClearDomainEvents();

        experience.MarkAsDeleted();

        experience.DomainEvents.Should().ContainSingle();
        var deletedEvent = experience.DomainEvents.First();
        deletedEvent.Should().BeOfType<WorkExperienceDeletedDomainEvent>();
        ((WorkExperienceDeletedDomainEvent)deletedEvent).WorkExperienceId.Should().Be(experience.Id);
    }

    [Fact]
    public void Publish_ShouldSetIsPublicToTrue()
    {
        var experience = WorkExperience.Create(
            "Corp", "Dev", new DateTime(2023, 1, 1), null, false);

        experience.Publish();

        experience.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Hide_ShouldSetIsPublicToFalse()
    {
        var experience = WorkExperience.Create(
            "Corp", "Dev", new DateTime(2023, 1, 1), null, true);

        experience.Hide();

        experience.IsPublic.Should().BeFalse();
    }
}
