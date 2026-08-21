using FluentValidation;

namespace JJDevHub.Content.Application.Commands.AddWorkExperience;

public class AddWorkExperienceCommandValidator : AbstractValidator<AddWorkExperienceCommand>
{
    public AddWorkExperienceCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.COMPANY_NAME_EMPTY")
            .MaximumLength(200)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.COMPANY_NAME_MAX");

        RuleFor(x => x.Position)
            .NotEmpty()
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.POSITION_EMPTY")
            .MaximumLength(200)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.POSITION_MAX");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.START_DATE_FUTURE");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.END_BEFORE_START");
    }
}
