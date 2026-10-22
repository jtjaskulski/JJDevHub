using System.Security.Claims;

using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.AspNetCore.Mvc;

namespace JJDevHub.Api.Auth
{
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Auth");
            group.MapPost("/register", RegisterAsync).WithName("Register").AllowAnonymous();
            group.MapPost("/login", LoginAsync).WithName("Login").AllowAnonymous();
            group.MapGet("/me", MeAsync).WithName("Me").RequireAuthorization();
            group.MapPut("/me", RenameAsync).WithName("RenameMe").RequireAuthorization();
            return group;
        }

        private static async Task<IResult> RegisterAsync(
            [FromBody] RegisterRequest request,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new RegisterAccountCommand(request.Email, request.Password, request.InviteCode),
                cancellationToken);
            if (result.TryGetValue(out var session))
            {
                return Results.Created("/api/auth/me", new AuthResponse(session.Token, session.ExpiresAt));
            }

            if (result.Error == ResultError.Conflict)
            {
                return Results.Conflict();
            }

            if (EmailAddressOk(request))
            {
                return FieldProblem("inviteCode", "Invite code is not valid.");
            }

            return FieldProblem("email", "Email and password are required.");
        }

        private static async Task<IResult> LoginAsync(
            [FromBody] LoginRequest request,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new LoginAccountCommand(request.Email, request.Password),
                cancellationToken);
            if (!result.TryGetValue(out var session))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new AuthResponse(session.Token, session.ExpiresAt));
        }

        private static async Task<IResult> MeAsync(
            ClaimsPrincipal principal,
            HttpContext http,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            if (Subject(principal) is not Guid accountId)
            {
                return Results.Unauthorized();
            }

            var result = await mediator.Send(new GetAccountQuery(accountId), cancellationToken);
            if (!result.TryGetValue(out var profile))
            {
                return Results.Unauthorized();
            }

            http.Response.Headers.ETag = AccountETag.Format(profile.Version);
            return Results.Ok(ToMe(profile));
        }

        private static async Task<IResult> RenameAsync(
            [FromBody] RenameRequest request,
            ClaimsPrincipal principal,
            HttpContext http,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            if (Subject(principal) is not Guid accountId)
            {
                return Results.Unauthorized();
            }

            if (!AccountETag.TryRead(http.Request.Headers.IfMatch, out var version))
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            var result = await mediator.Send(
                new RenameAccountCommand(accountId, request.DisplayName, version),
                cancellationToken);
            if (result.TryGetValue(out var profile))
            {
                http.Response.Headers.ETag = AccountETag.Format(profile.Version);
                return Results.Ok(ToMe(profile));
            }

            if (result.Error == ResultError.Conflict)
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            return FieldProblem("displayName", "Display name must be between 2 and 50 characters.");
        }

        private static bool EmailAddressOk(RegisterRequest request) =>
            !string.IsNullOrWhiteSpace(request.Email)
            && request.Email.Contains('@')
            && !string.IsNullOrEmpty(request.Password)
            && request.Password.Length >= 8;

        private static Guid? Subject(ClaimsPrincipal principal)
        {
            var sub = principal.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var accountId))
            {
                return accountId;
            }

            return null;
        }

        private static MeResponse ToMe(AccountProfile profile) =>
            new(profile.AccountId, profile.Email, profile.DisplayName, profile.Roles);

        private static IResult FieldProblem(string field, string message) =>
            Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [field] = [message],
            });
    }
}
