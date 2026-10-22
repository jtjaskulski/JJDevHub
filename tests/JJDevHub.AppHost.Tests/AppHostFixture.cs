using Aspire.Hosting;
using Aspire.Hosting.Testing;

using JJDevHub.AppHost;

namespace JJDevHub.AppHost.Tests
{
    public sealed class AppHostFixture : IAsyncLifetime
    {
        private DistributedApplication? _app;

        public DistributedApplication App
        {
            get
            {
                if (_app is null)
                {
                    throw new InvalidOperationException("App host is not started.");
                }

                return _app;
            }
        }

        public async Task InitializeAsync()
        {
            var builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.JJDevHub_AppHost>();
            builder.Configuration["Parameters:signing-key-path"] = "";
            builder.Configuration["Parameters:owner-email"] = "";

            _app = await builder.BuildAsync();
            await _app.StartAsync();

            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.IdentityApi,
                timeout.Token);
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.NewsWrite,
                timeout.Token);
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.NewsRead,
                timeout.Token);
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.IdentityWrite,
                timeout.Token);
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                ResourceNames.IdentityRead,
                timeout.Token);
        }

        public async Task DisposeAsync()
        {
            if (_app is not null)
            {
                await _app.DisposeAsync();
            }
        }
    }
}