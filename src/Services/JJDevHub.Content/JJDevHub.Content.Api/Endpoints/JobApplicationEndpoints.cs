using JJDevHub.Content.Application.Commands.AddJobApplicationInterviewStage;
using JJDevHub.Content.Application.Commands.AddJobApplicationNote;
using JJDevHub.Content.Application.Commands.AddJobApplicationRequirement;
using JJDevHub.Content.Application.Commands.CreateJobApplication;
using JJDevHub.Content.Application.Commands.DeleteJobApplication;
using JJDevHub.Content.Application.Commands.UpdateJobApplication;
using JJDevHub.Content.Application.Queries.GetJobApplicationById;
using JJDevHub.Content.Application.Queries.GetJobApplicationDashboard;
using JJDevHub.Content.Application.Queries.GetJobApplications;
using JJDevHub.Content.Core.Enums;
using MediatR;

namespace JJDevHub.Content.Api.Endpoints;

public static class JobApplicationEndpoints
{
    public static RouteGroupBuilder MapJobApplicationEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/applications")
            .WithTags("JobApplications")
            .RequireAuthorization("OwnerOnly");

        group.MapGet("/", GetAll);
        group.MapGet("/dashboard", GetDashboard);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).RequireRateLimiting("writes");
        group.MapPut("/{id:guid}", Update).RequireRateLimiting("writes");
        group.MapDelete("/{id:guid}", Delete).RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/requirements", AddRequirement).RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/notes", AddNote).RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/interview-stages", AddInterviewStage).RequireRateLimiting("writes");

        return group;
    }

    private static async Task<IResult> GetAll(
        IMediator mediator,
        ApplicationStatus? status,
        string? companyContains,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetJobApplicationsQuery(status, companyContains),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDashboard(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetJobApplicationDashboardQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetJobApplicationByIdQuery(id), cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> Create(
        CreateJobApplicationRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var id = await mediator.Send(
            new CreateJobApplicationCommand(
                body.CompanyName,
                body.Location,
                body.WebsiteUrl,
                body.Industry,
                body.Position,
                body.Status,
                body.AppliedDate,
                body.LinkedCurriculumVitaeId),
            cancellationToken);

        return Results.Created($"/api/v1/content/applications/{id}", new { id });
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateJobApplicationRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateJobApplicationCommand(
                id,
                body.Version,
                body.CompanyName,
                body.Location,
                body.WebsiteUrl,
                body.Industry,
                body.Position,
                body.Status,
                body.AppliedDate,
                body.LinkedCurriculumVitaeId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> Delete(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteJobApplicationCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddRequirement(
        Guid id,
        AddRequirementRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var reqId = await mediator.Send(
            new AddJobApplicationRequirementCommand(
                id,
                body.Version,
                body.Description,
                body.Category,
                body.Priority,
                body.IsMet),
            cancellationToken);

        return Results.Created($"/api/v1/content/applications/{id}/requirements/{reqId}", new { id = reqId });
    }

    private static async Task<IResult> AddNote(
        Guid id,
        AddNoteRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var noteId = await mediator.Send(
            new AddJobApplicationNoteCommand(
                id,
                body.Version,
                body.Content,
                body.NoteType,
                body.CreatedAt),
            cancellationToken);

        return Results.Created($"/api/v1/content/applications/{id}/notes/{noteId}", new { id = noteId });
    }

    private static async Task<IResult> AddInterviewStage(
        Guid id,
        AddInterviewStageRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var stageId = await mediator.Send(
            new AddJobApplicationInterviewStageCommand(
                id,
                body.Version,
                body.StageName,
                body.ScheduledAt,
                body.Status,
                body.Feedback),
            cancellationToken);

        return Results.Created($"/api/v1/content/applications/{id}/interview-stages/{stageId}", new { id = stageId });
    }
}

public record CreateJobApplicationRequest(
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId);

public record UpdateJobApplicationRequest(
    long Version,
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId);

public record AddRequirementRequest(
    long Version,
    string Description,
    RequirementCategory Category,
    RequirementPriority Priority,
    bool IsMet);

public record AddNoteRequest(
    long Version,
    string Content,
    ApplicationNoteType NoteType,
    DateTime CreatedAt);

public record AddInterviewStageRequest(
    long Version,
    string StageName,
    DateTime ScheduledAt,
    InterviewStageStatus Status,
    string? Feedback);
