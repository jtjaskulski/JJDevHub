using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Content.Persistence.Outbox;
using JJDevHub.Content.Persistence.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Content.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ContentDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "content")));

        services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ContentDbContext>());

        return services;
    }
}
