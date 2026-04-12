using JJDevHub.Content.Api.Services;
using JJDevHub.Content.Application.Commands.AddCurriculumVitaeEducation;
using JJDevHub.Content.Application.Commands.AddCurriculumVitaeProject;
using JJDevHub.Content.Application.Commands.AddCurriculumVitaeSkill;
using JJDevHub.Content.Application.Commands.CreateCurriculumVitae;
using JJDevHub.Content.Application.Commands.DeleteCurriculumVitae;
using JJDevHub.Content.Application.Commands.LinkCurriculumVitaeWorkExperience;
using JJDevHub.Content.Application.Commands.RemoveCurriculumVitaeSkill;
using JJDevHub.Content.Application.Commands.UpdateCurriculumVitaePersonalInfo;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.Queries.GetCurriculumVitaeById;
using JJDevHub.Content.Application.Queries.GetCurriculumVitaes;
using JJDevHub.Content.Application.ReadModels;
using JJDevHub.Content.Core.Enums;
using JJDevHub.Content.Api.Middleware;
using MediatR;

namespace JJDevHub.Content.Api.Endpoints;

public static class CurriculumVitaeEndpoints
{
    public static RouteGroupBuilder MapCurriculumVitaeEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/cv")
            .WithTags("CurriculumVitae");

        group.MapGet("/", GetAll).AllowAnonymous();
        group.MapGet("/{id:guid}", GetById).AllowAnonymous();
        group.MapPost("/", Create)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes")
            .WithDescription(
                "Example body: {\"firstName\":\"Jan\",\"lastName\":\"Kowalski\"," +
                "\"email\":\"jan@example.com\",\"phone\":\"+48111222333\",\"location\":\"Warsaw\",\"bio\":\"...\"}");
        group.MapPut("/{id:guid}", UpdatePersonalInfo)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/skills", AddSkill)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapDelete("/{id:guid}/skills/{skillId:guid}", RemoveSkill)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/educations", AddEducation)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/projects", AddProject)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/work-experiences", LinkWorkExperience)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapPost("/{id:guid}/pdf", GeneratePdf)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapGet("/pdf-download/{fileId:guid}", DownloadPdf)
            .RequireAuthorization("OwnerOnly");

        return group;
    }

    private static async Task<IResult> GetAll(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurriculumVitaesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurriculumVitaeByIdQuery(id), cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> Create(
        CreateCurriculumVitaeRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCurriculumVitaeCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Location,
            request.Bio);

        var id = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/content/cv/{id}", new { id });
    }

    private static async Task<IResult> UpdatePersonalInfo(
        Guid id,
        UpdateCurriculumVitaePersonalInfoRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCurriculumVitaePersonalInfoCommand(
            id,
            request.Version,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Location,
            request.Bio);

        await mediator.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Delete(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCurriculumVitaeCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddSkill(
        Guid id,
        AddCurriculumVitaeSkillRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new AddCurriculumVitaeSkillCommand(
                id,
                request.Version,
                request.Name,
                request.Category,
                request.Level),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveSkill(
        Guid id,
        Guid skillId,
        long? version,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (version is null)
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION.FAILED",
                null,
                new[] { new ValidationErrorItem("version", "VALIDATION.REQUIRED", "Query parameter 'version' is required.") }));

        await mediator.Send(
            new RemoveCurriculumVitaeSkillCommand(id, version.Value, skillId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddEducation(
        Guid id,
        AddCurriculumVitaeEducationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new AddCurriculumVitaeEducationCommand(
                id,
                request.Version,
                request.Institution,
                request.FieldOfStudy,
                request.Degree,
                request.PeriodStart,
                request.PeriodEnd),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddProject(
        Guid id,
        AddCurriculumVitaeProjectRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new AddCurriculumVitaeProjectCommand(
                id,
                request.Version,
                request.Name,
                request.Description,
                request.Url,
                request.Technologies,
                request.PeriodStart,
                request.PeriodEnd),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LinkWorkExperience(
        Guid id,
        LinkCurriculumVitaeWorkExperienceRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new LinkCurriculumVitaeWorkExperienceCommand(
                id,
                request.Version,
                request.WorkExperienceId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GeneratePdf(
        Guid id,
        GenerateCvPdfRequest? body,
        IMediator mediator,
        IJobApplicationReadStore jobApplications,
        ICvPdfBlobStore pdfStore,
        CancellationToken cancellationToken)
    {
        var cv = await mediator.Send(new GetCurriculumVitaeByIdQuery(id), cancellationToken);
        if (cv is null)
            return Results.NotFound();

        JobApplicationReadModel? job = null;
        if (body?.JobApplicationId is { } jobId)
        {
            job = await jobApplications.GetByIdAsync(jobId, cancellationToken);
            if (job is null)
                return Results.BadRequest("Job application not found.");
        }

        var bytes = CurriculumVitaePdfComposer.Compose(cv, job);
        var fileName = $"cv-{cv.PersonalInfo.LastName}-{cv.Id:N}.pdf".ToLowerInvariant();
        var fileId = await pdfStore.SaveAsync(id, body?.JobApplicationId, fileName, bytes, cancellationToken);

        return Results.Ok(new GenerateCvPdfResponse(
            fileId,
            $"/api/v1/content/cv/pdf-download/{fileId}"));
    }

    private static async Task<IResult> DownloadPdf(
        Guid fileId,
        ICvPdfBlobStore pdfStore,
        CancellationToken cancellationToken)
    {
        var blob = await pdfStore.GetAsync(fileId, cancellationToken);
        if (blob is null)
            return Results.NotFound();

        var (content, fileName) = blob.Value;
        return Results.File(content, "application/pdf", fileName);
    }
}

public record GenerateCvPdfRequest(Guid? JobApplicationId);

public record GenerateCvPdfResponse(Guid FileId, string DownloadPath);

public record CreateCurriculumVitaeRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio);

public record UpdateCurriculumVitaePersonalInfoRequest(
    long Version,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Location,
    string? Bio);

public record AddCurriculumVitaeSkillRequest(
    long Version,
    string Name,
    string Category,
    SkillLevel Level);

public record AddCurriculumVitaeEducationRequest(
    long Version,
    string Institution,
    string FieldOfStudy,
    EducationDegree Degree,
    DateTime PeriodStart,
    DateTime? PeriodEnd);

public record AddCurriculumVitaeProjectRequest(
    long Version,
    string Name,
    string Description,
    string? Url,
    IReadOnlyList<string> Technologies,
    DateTime PeriodStart,
    DateTime? PeriodEnd);

public record LinkCurriculumVitaeWorkExperienceRequest(long Version, Guid WorkExperienceId);
