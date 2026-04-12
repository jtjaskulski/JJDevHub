using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.AddJobApplicationInterviewStage;

public record AddJobApplicationInterviewStageCommand(
    Guid JobApplicationId,
    long ExpectedVersion,
    string StageName,
    DateTime ScheduledAt,
    InterviewStageStatus Status,
    string? Feedback) : ICommand<Guid>;
