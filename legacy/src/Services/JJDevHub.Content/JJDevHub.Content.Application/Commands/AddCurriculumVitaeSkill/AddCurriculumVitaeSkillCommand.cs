using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeSkill;

public record AddCurriculumVitaeSkillCommand(
    Guid CurriculumVitaeId,
    long ExpectedVersion,
    string Name,
    string Category,
    SkillLevel Level) : ICommand<Unit>;
