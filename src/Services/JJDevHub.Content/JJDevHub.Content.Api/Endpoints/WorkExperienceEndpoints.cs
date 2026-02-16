using JJDevHub.Content.Application.Commands.AddWorkExperience;
using JJDevHub.Content.Application.Commands.DeleteWorkExperience;
using JJDevHub.Content.Application.Commands.UpdateWorkExperience;
using JJDevHub.Content.Application.Queries.GetWorkExperienceById;
using JJDevHub.Content.Application.Queries.GetWorkExperiences;
using MediatR;

namespace JJDevHub.Content.Api.Endpoints;

public static class WorkExperienceEndpoints
{
    public static IEndpointRouteBuilder MapWorkExperienceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/content/work-experiences")
            .WithTags("WorkExperiences");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
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
        return Results.Created($"/api/content/work-experiences/{id}", new { id });
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateWorkExperienceRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkExperienceCommand(
            id,
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
    string CompanyName,
    string Position,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPublic);
