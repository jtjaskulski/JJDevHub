using JJDevHub.AppHost;
using JJDevHub.EventBus.Kafka;
using JJDevHub.Identity.Contracts;
using JJDevHub.IntegrationEventLog;
using JJDevHub.News.Contracts;
using JJDevHub.Notifications.Worker.Data;
using JJDevHub.Notifications.Worker.IntegrationEvents;
using JJDevHub.Notifications.Worker.Migrations;
using JJDevHub.Notifications.Worker.Sending;
using JJDevHub.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection.Extensions;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<NotificationsContext>(
    ResourceNames.Notifications,
    settings => settings.DisableRetry = true);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevelopmentMigrator>();
}

builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services
    .AddIntegrationEventLog<NotificationsContext>(
        [typeof(UserRegisteredIntegrationEvent).Assembly, typeof(ArticleCreatedIntegrationEvent).Assembly],
        options => options.PublishOutbox = false)
    .AddIntegrationEventHandlers(typeof(WelcomeUserHandler).Assembly);
builder.Services.Configure<InboxOptions>(options => options.Consumer = "notifications");
builder.Services.AddSingleton<INotificationSender, LoggingNotificationSender>();
builder.Services.AddKafkaConsumer(builder.Configuration, options =>
{
    options.ConsumerGroup = "notifications";
    options.SubscribedTopics = ["identity.integration", "news.integration"];
});

WebApplication app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
