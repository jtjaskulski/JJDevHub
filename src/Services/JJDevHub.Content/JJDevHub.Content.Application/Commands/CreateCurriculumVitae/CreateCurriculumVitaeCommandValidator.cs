using FluentValidation;

namespace JJDevHub.Content.Application.Commands.CreateCurriculumVitae;

public class CreateCurriculumVitaeCommandValidator : AbstractValidator<CreateCurriculumVitaeCommand>
{
    public CreateCurriculumVitaeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Bio).MaximumLength(4000);
    }
}
