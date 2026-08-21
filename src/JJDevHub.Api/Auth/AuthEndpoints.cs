using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JJDevHub.Api.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<AuthResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", Me)
            .WithName("Me")
            .RequireAuthorization()
            .Produces<MeResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        UserManager<IdentityUser> users,
        TokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email and password are required."]
            });
        }

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return Results.ValidationProblem(errors);
        }

        var token = tokens.CreateToken(user);
        return Results.Created("/api/auth/me", new AuthResponse(token, tokens.GetExpiry()));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        UserManager<IdentityUser> users,
        TokenService tokens)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        {
            return Results.Unauthorized();
        }

        var token = tokens.CreateToken(user);
        return Results.Ok(new AuthResponse(token, tokens.GetExpiry()));
    }

    private static IResult Me(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(id))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new MeResponse(id, email));
    }
}
