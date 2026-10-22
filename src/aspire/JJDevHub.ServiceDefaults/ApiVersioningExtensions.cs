using Asp.Versioning;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.ServiceDefaults
{
    public static class ApiVersioningExtensions
    {
        public static IServiceCollection AddDefaultApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("api-version");
            });
            return services;
        }
    }
}
