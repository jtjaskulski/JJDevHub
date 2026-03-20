using FluentValidation;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeSkill;

public class AddCurriculumVitaeSkillCommandValidator : AbstractValidator<AddCurriculumVitaeSkillCommand>
{
    public AddCurriculumVitaeSkillCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}
