using Learning.Application.UseCases.Handlers;

using Microsoft.Extensions.DependencyInjection;

namespace Learning.Application
{
    public static class LearningApplication
    {
        public static IServiceCollection AddLearningApplication(this IServiceCollection services)
        {
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<PublishTechnologyHandler>();
            services.AddScoped<PublishCourseHandler>();
            services.AddScoped<PublishLessonHandler>();
            services.AddScoped<ReviseLessonHandler>();
            services.AddScoped<CompleteLessonHandler>();
            return services;
        }
    }
}