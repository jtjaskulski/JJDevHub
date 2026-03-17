using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Infrastructure.Messaging;
using JJDevHub.Content.Infrastructure.ReadStore;
using JJDevHub.Shared.Kernel.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

using System.Threading;

namespace JJDevHub.Content.Infrastructure;

public static class DependencyInjection
{
    private static int _guidSerializerRegistered;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        if (Interlocked.Exchange(ref _guidSerializerRegistered, 1) == 0)
        {
#pragma warning disable CS0618
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore CS0618
        }

        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton<IWorkExperienceReadStore, MongoWorkExperienceReadStore>();

        services.AddSingleton<IEventBus, KafkaEventBus>();
        services.AddHostedService<OutboxPublisherHostedService>();

        return services;
    }
}
