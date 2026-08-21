using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class InterviewStage : Entity
{
    public Guid JobApplicationId { get; internal set; }

    public string StageName { get; private set; } = null!;
    public DateTime ScheduledAt { get; private set; }
    public InterviewStageStatus Status { get; private set; }
    public string? Feedback { get; private set; }

    private InterviewStage() { }

    internal InterviewStage(string stageName, DateTime scheduledAt, InterviewStageStatus status, string? feedback)
    {
        StageName = ValidateStageName(stageName);
        ScheduledAt = scheduledAt;
        Status = status;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
    }

    public void Update(string stageName, DateTime scheduledAt, InterviewStageStatus status, string? feedback)
    {
        StageName = ValidateStageName(stageName);
        ScheduledAt = scheduledAt;
        Status = status;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
    }

    private static string ValidateStageName(string stageName)
    {
        if (string.IsNullOrWhiteSpace(stageName))
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.STAGE_EMPTY", "Stage name is required.");
        if (stageName.Length > 200)
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.STAGE_MAX", "Stage name is too long.");
        return stageName.Trim();
    }
}
