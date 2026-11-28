using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Shouldly;

namespace JJDevHub.Gateway.Tests
{
    public sealed class VersionsTests
    {
        [Fact]
        public async Task Versions_returns_six_fields_when_neighbors_are_down()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            await using var factory = new GatewayFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/versions", cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            root.EnumerateObject().Count().ShouldBe(6);
            root.GetProperty("identity").GetString().ShouldBe("");
            root.GetProperty("news").GetString().ShouldBe("");
            root.GetProperty("learning").GetString().ShouldBe("");
            root.GetProperty("cv").GetString().ShouldBe("");
            root.GetProperty("contact").GetString().ShouldBe("");
            root.GetProperty("gateway").GetString().ShouldBe("gateway-v0.1.0");
        }

        private sealed class GatewayFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(NeighborOverrides());
                });
            }

            private static Dictionary<string, string?> NeighborOverrides()
            {
                const string closed = "http://127.0.0.1:1";

                return new Dictionary<string, string?>
                {
                    ["SERVICE_VERSION"] = "gateway-v0.1.0",
                    ["ReverseProxy:Clusters:identity:Destinations:destination:Address"] = closed,
                    ["ReverseProxy:Clusters:news:Destinations:destination:Address"] = closed,
                    ["ReverseProxy:Clusters:learning:Destinations:destination:Address"] = closed,
                    ["ReverseProxy:Clusters:cv:Destinations:destination:Address"] = closed,
                    ["ReverseProxy:Clusters:contact:Destinations:destination:Address"] = closed,
                };
            }
        }
    }
}