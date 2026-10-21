using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JJDevHub.ServiceDefaults
{

    public sealed class ExceptionHandlingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                UseExceptionPage(app);
                next(app);
            };
        }

        private static void UseExceptionPage(IApplicationBuilder app)
        {
            var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                return;
            }

            app.UseExceptionHandler();
        }
    }
}
