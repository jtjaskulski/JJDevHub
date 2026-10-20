using JJDevHub.Cqrs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JJDevHub.Cqrs.Tests
{

    internal static class TestServices
    {
        public static ServiceCollection Create()
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddCqrs(typeof(TestServices).Assembly);
            return services;
        }
    }
}
