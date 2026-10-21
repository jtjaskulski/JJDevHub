using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

using JJDevHub.ServiceDefaults;

namespace JJDevHub.AppHost.Tests
{
    public sealed class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task Unhandled_exception_returns_problem_500()
        {
            var response = await GetBoom();

            Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
            Assert.Equal("application/problem+json", response.MediaType);
        }

        [Fact]
        public async Task Problem_has_trace_id()
        {
            var response = await GetBoom();

            using var document = JsonDocument.Parse(response.Body);
            var traceId = document.RootElement.GetProperty("traceId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(traceId));
        }

        [Fact]
        public async Task Problem_hides_exception_message()
        {
            var response = await GetBoom();

            Assert.DoesNotContain("secret detail", response.Body);
        }

        private static async Task<BoomResponse> GetBoom()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Environment.EnvironmentName = Environments.Production;
            builder.WebHost.UseTestServer();
            builder.AddServiceDefaults();

            await using var app = builder.Build();
            app.MapGet("/boom", () =>
            {
                throw new InvalidOperationException("secret detail");
            });
            app.MapDefaultEndpoints();
            await app.StartAsync();

            var response = await app.GetTestClient().GetAsync("/boom");
            var body = await response.Content.ReadAsStringAsync();
            return new BoomResponse(
                response.StatusCode,
                response.Content.Headers.ContentType?.MediaType,
                body);
        }

        private sealed record BoomResponse(HttpStatusCode Status, string? MediaType, string Body);
    }
}