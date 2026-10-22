using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.ServiceDefaults;

namespace JJDevHub.Api.Admin
{
    public static class UserAdminEndpoints
    {
        public static RouteGroupBuilder MapUserAdminEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth/users").WithTags("Users").RequireAuthorization(Policies.Owner);
            group.MapGet("", ListAsync);
            group.MapPut("/{id:guid}/roles/student", GrantAsync);
            group.MapDelete("/{id:guid}/roles/student", RevokeAsync);
            return group;
        }

        private static async Task<IResult> ListAsync(
            string? role,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new ListUsersQuery(role), cancellationToken);
            if (!result.TryGetValue(out var users))
            {
                return Results.BadRequest();
            }

            return Results.Ok(users);
        }

        private static Task<IResult> GrantAsync(Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            ChangeRoleAsync(new GrantStudentRoleCommand(id), mediator, cancellationToken);

        private static Task<IResult> RevokeAsync(Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            ChangeRoleAsync(new RevokeStudentRoleCommand(id), mediator, cancellationToken);

        private static async Task<IResult> ChangeRoleAsync(
            IRequest<Guid> command,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            if (result.Error == ResultError.NotFound)
            {
                return Results.NotFound();
            }

            if (!result.Succeeded)
            {
                return Results.BadRequest();
            }

            return Results.NoContent();
        }
    }
}
