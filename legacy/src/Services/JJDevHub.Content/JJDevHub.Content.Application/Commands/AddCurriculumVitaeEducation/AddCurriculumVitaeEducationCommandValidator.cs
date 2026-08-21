using FluentValidation;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeEducation;

public class AddCurriculumVitaeEducationCommandValidator
    : AbstractValidator<AddCurriculumVitaeEducationCommand>
{
    public AddCurriculumVitaeEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FieldOfStudy).NotEmpty().MaximumLength(300);
    }
}
