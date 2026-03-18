using FluentValidation;

namespace JJDevHub.Content.Application.Commands.UpdateWorkExperience;

public class UpdateWorkExperienceCommandValidator : AbstractValidator<UpdateWorkExperienceCommand>
{
    public UpdateWorkExperienceCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.VERSION_REQUIRED")
            .WithMessage("ExpectedVersion must be greater than 0.");

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
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.START_DATE_FUTURE");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithErrorCode("VALIDATION.WORK_EXPERIENCE.END_BEFORE_START");
    }
}
