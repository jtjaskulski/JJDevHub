using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Infrastructure.Messaging;
using JJDevHub.Content.Infrastructure.ReadStore;
using JJDevHub.Shared.Kernel.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });
        services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();

        services.AddSingleton<IEventBus, KafkaEventBus>();
        services.AddHostedService<OutboxPublisherHostedService>();

        return services;
    }
}
