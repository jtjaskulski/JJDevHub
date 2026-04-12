using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.RemoveCurriculumVitaeSkill;

public record RemoveCurriculumVitaeSkillCommand(
    Guid CurriculumVitaeId,
    long ExpectedVersion,
    Guid SkillId) : ICommand<Unit>;
