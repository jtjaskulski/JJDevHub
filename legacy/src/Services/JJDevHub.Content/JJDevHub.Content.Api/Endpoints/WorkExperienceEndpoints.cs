using JJDevHub.Content.Application.Commands.AddWorkExperience;
using JJDevHub.Content.Application.Commands.DeleteWorkExperience;
using JJDevHub.Content.Application.Commands.UpdateWorkExperience;
using JJDevHub.Content.Application.Queries.GetWorkExperienceById;
using JJDevHub.Content.Application.Queries.GetWorkExperiences;
using MediatR;

namespace JJDevHub.Content.Api.Endpoints;

public static class WorkExperienceEndpoints
{
    public static RouteGroupBuilder MapWorkExperienceEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/work-experiences")
            .WithTags("WorkExperiences");

        group.MapGet("/", GetAll)
            .AllowAnonymous()
            .CacheOutput("PublicWorkExperiences")
            .WithDescription(
                "List work experiences. Query publicOnly=true for public entries only. " +
                "Example: GET .../work-experiences?publicOnly=true");
        group.MapGet("/{id:guid}", GetById).AllowAnonymous();
        group.MapPost("/", Create)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes")
            .WithDescription(
                "Example body: {\"companyName\":\"Contoso\",\"position\":\"Engineer\"," +
                "\"startDate\":\"2022-01-01T00:00:00Z\",\"endDate\":null,\"isPublic\":true}");
        group.MapPut("/{id:guid}", Update)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");
        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization("OwnerOnly")
            .RequireRateLimiting("writes");

        return group;
    }

    private static async Task<IResult> GetAll(
        IMediator mediator,
        bool? publicOnly,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkExperiencesQuery(publicOnly ?? false);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkExperienceByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> Create(
        AddWorkExperienceCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/content/work-experiences/{id}", new { id });
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateWorkExperienceRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkExperienceCommand(
            id,
            request.Version,
            request.CompanyName,
            request.Position,
            request.StartDate,
            request.EndDate,
            request.IsPublic);

        await mediator.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Delete(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteWorkExperienceCommand(id), cancellationToken);
        return Results.NoContent();
    }
}

public record UpdateWorkExperienceRequest(
    long Version,
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic);