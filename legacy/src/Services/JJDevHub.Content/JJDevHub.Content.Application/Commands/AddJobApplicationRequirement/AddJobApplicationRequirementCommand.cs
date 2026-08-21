using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationRequirement;

public record AddJobApplicationRequirementCommand(
    Guid JobApplicationId,
    long ExpectedVersion,
    string Description,
    RequirementCategory Category,
    RequirementPriority Priority,
    bool IsMet) : ICommand<Guid>;
