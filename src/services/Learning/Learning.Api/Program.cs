using Learning.Application;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
AddLearning(builder);

if (IsMigrate(args))
{
    await using WebApplication host = builder.Build();
    await MigrateAsync(host, CancellationToken.None);
    return;
}

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await MigrateAsync(app, CancellationToken.None);
}

app.MapDefaultEndpoints();
app.Run();

static bool IsMigrate(string[] args)
{
    foreach (string arg in args)
    {
        if (arg == "migrate")
        {
            return true;
        }
    }

    return false;
}

static void AddLearning(WebApplicationBuilder builder)
{
    string? connectionString = builder.Configuration.GetConnectionString("learning-write");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Brak connection stringa learning-write.");
    }

    builder.Services.AddLearningApplication();
    builder.Services.AddLearningPersistence(connectionString);
}

static async Task MigrateAsync(WebApplication app, CancellationToken cancellationToken)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    LearningDbContext context = scope.ServiceProvider.GetRequiredService<LearningDbContext>();
    await context.Database.MigrateAsync(cancellationToken);
}

public partial class Program
{
}