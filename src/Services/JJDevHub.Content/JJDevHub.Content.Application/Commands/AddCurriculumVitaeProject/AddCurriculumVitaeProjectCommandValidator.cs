using FluentValidation;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeProject;

public class AddCurriculumVitaeProjectCommandValidator : AbstractValidator<AddCurriculumVitaeProjectCommand>
{
    public AddCurriculumVitaeProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Url).MaximumLength(2000);
        RuleForEach(x => x.Technologies).MaximumLength(100);
    }
}
