using FluentValidation.TestHelper;
using JJDevHub.Content.Application.Commands.AddWorkExperience;

namespace JJDevHub.Content.UnitTests.Application;

public class AddWorkExperienceCommandValidatorTests
{
    private readonly AddWorkExperienceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            new DateTime(2023, 1, 1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCompanyName_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            "", "Developer",
            new DateTime(2023, 1, 1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Validate_WithCompanyNameTooLong_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            new string('A', 201), "Developer",
            new DateTime(2023, 1, 1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Validate_WithEmptyPosition_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "",
            new DateTime(2023, 1, 1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Position);
    }

    [Fact]
    public void Validate_WithFutureStartDate_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            DateTime.UtcNow.AddYears(1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_ShouldFail()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            new DateTime(2024, 1, 1),
            new DateTime(2023, 1, 1),
            true);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_WithNullEndDate_ShouldPass()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            new DateTime(2023, 1, 1), null, true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }
}
