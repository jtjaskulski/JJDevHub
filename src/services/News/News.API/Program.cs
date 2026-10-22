using JJDevHub.AppHost;
using JJDevHub.EventBus.Kafka;
using JJDevHub.IntegrationEventLog;
using JJDevHub.News.Api.Articles;
using JJDevHub.News.Api.Http;
using JJDevHub.News.Api.Migrations;
using JJDevHub.News.Application;
using JJDevHub.News.Infrastructure;
using JJDevHub.News.Infrastructure.Persistence;
using JJDevHub.News.Infrastructure.Read;
using JJDevHub.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAuthentication();
builder.AddNpgsqlDbContext<NewsWriteContext>(ResourceNames.NewsWrite, settings => settings.DisableRetry = true);
builder.AddNpgsqlDbContext<NewsReadContext>(ResourceNames.NewsRead);
builder.AddKafkaProducer<string, string>(ResourceNames.Kafka);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevelopmentMigrator>();
}

builder.Services
    .AddNewsApplication()
    .AddNewsWrite()
    .AddNewsRead()
    .AddKafkaEventBus(builder.Configuration, options => options.PublishedTopics = ["news.integration"])
    .AddDefaultApiVersioning();
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();

WebApplication app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapArticles();
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
