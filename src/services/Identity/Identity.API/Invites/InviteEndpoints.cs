using System.Security.Claims;

using JJDevHub.Api.Auth;
using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.ServiceDefaults;

using Microsoft.AspNetCore.Mvc;

namespace JJDevHub.Api.Invites
{
    public static class InviteEndpoints
    {
        public static RouteGroupBuilder MapInviteEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth/invites").WithTags("Invites");
            group.MapPost("", CreateAsync).RequireAuthorization(Policies.Owner);
            group.MapGet("", ListAsync).RequireAuthorization(Policies.Owner);
            group.MapDelete("/{id:guid}", RevokeAsync).RequireAuthorization(Policies.Owner);
            group.MapPost("/redeem", RedeemAsync).RequireAuthorization();
            return group;
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] CreateInviteRequest request,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new CreateInviteCommand(request.Label, request.ExpiresOn, request.MaxUses),
                cancellationToken);
            if (!result.TryGetValue(out var created))
            {
                return Results.BadRequest();
            }

            return Results.Created($"/api/auth/invites/{created.InviteId}", new { code = created.Code });
        }

        private static async Task<IResult> ListAsync(IMediator mediator, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new ListInvitesQuery(), cancellationToken);
            if (!result.TryGetValue(out var items))
            {
                return Results.BadRequest();
            }

            return Results.Ok(items);
        }

        private static async Task<IResult> RevokeAsync(
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new RevokeInviteCommand(id), cancellationToken);
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

        private static async Task<IResult> RedeemAsync(
            [FromBody] RedeemInviteRequest request,
            ClaimsPrincipal principal,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            if (Subject(principal) is not Guid accountId)
            {
                return Results.Unauthorized();
            }

            var result = await mediator.Send(new RedeemInviteCommand(accountId, request.Code), cancellationToken);
            if (!result.TryGetValue(out var session))
            {
                return Results.BadRequest();
            }

            return Results.Ok(new AuthResponse(session.Token, session.ExpiresAt));
        }

        private static Guid? Subject(ClaimsPrincipal principal)
        {
            var sub = principal.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var accountId))
            {
                return accountId;
            }

            return null;
        }
    }
}
