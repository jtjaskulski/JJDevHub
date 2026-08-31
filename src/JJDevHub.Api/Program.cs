using System.Reflection;
using System.Text;
using JJDevHub.Api.Auth;
using JJDevHub.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<TokenService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT from POST /api/auth/login",
        };
        return Task.CompletedTask;
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (!IsOpenApiDocumentGeneration(args))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
}))
.WithName("Health")
.WithTags("Health");

app.MapAuthEndpoints();

app.Run();

static bool IsOpenApiDocumentGeneration(string[] args)
{
    if (args.Any(a => a.Contains("GetDocument", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    var process = Environment.ProcessPath ?? string.Empty;
    if (process.Contains("getdocument", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var entry = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
    return entry.Contains("getdocument", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
