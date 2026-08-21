using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.LinkCurriculumVitaeWorkExperience;

public record LinkCurriculumVitaeWorkExperienceCommand(
    Guid CurriculumVitaeId,
    long ExpectedVersion,
    Guid WorkExperienceId) : ICommand<Unit>;
