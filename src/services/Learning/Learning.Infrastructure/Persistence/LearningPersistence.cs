using EFCore.NamingConventions;

using Learning.Application.Ports;
using Learning.Infrastructure.Adapters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Learning.Infrastructure.Persistence
{
    public static class LearningPersistence
    {
        public static IServiceCollection AddLearningPersistence(
            this IServiceCollection services,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Brak connection stringa learning-write.");
            }

            services.AddDbContext<LearningDbContext>(options =>
            {
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            });
            services.AddScoped<ICategoryRepository, EfCategoryRepository>();
            services.AddScoped<ITechnologyRepository, EfTechnologyRepository>();
            services.AddScoped<ICourseRepository, EfCourseRepository>();
            services.AddScoped<ILessonRepository, EfLessonRepository>();
            services.AddScoped<IStudentProgressStore, EfStudentProgressStore>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            return services;
        }
    }
}