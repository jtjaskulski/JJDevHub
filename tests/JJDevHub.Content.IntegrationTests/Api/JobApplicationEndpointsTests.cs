using System.Net;
using System.Net.Http.Json;
using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.IntegrationTests.Fixtures;

namespace JJDevHub.Content.IntegrationTests.Api;

public class JobApplicationEndpointsTests : IClassFixture<ContentApiFactory>, IAsyncLifetime
{
    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public JobApplicationEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await DatabaseFixture.ResetDatabaseAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateJobApplication_ShouldReturn201()
    {
        var body = new
        {
            companyName = "Integration Corp",
            location = "Gdansk",
            websiteUrl = (string?)null,
            industry = "IT",
            position = "Engineer",
            status = "Applied",
            appliedDate = "2024-06-01",
            linkedCurriculumVitaeId = (Guid?)null
        };

        var response = await _client.PostAsJsonAsync("/api/v1/content/applications", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        json!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetJobApplications_AfterCreate_ShouldReturnList()
    {
        await _client.PostAsJsonAsync("/api/v1/content/applications", new
        {
            companyName = "ListCo",
            location = (string?)null,
            websiteUrl = (string?)null,
            industry = (string?)null,
            position = "Dev",
            status = "Applied",
            appliedDate = "2024-01-15",
            linkedCurriculumVitaeId = (Guid?)null
        });

        var response = await _client.GetAsync("/api/v1/content/applications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<JobApplicationDto>>();
        list.Should().NotBeNull();
        list!.Should().Contain(x => x.CompanyName == "ListCo");
    }

    [Fact]
    public async Task GetJobApplicationDashboard_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/v1/content/applications/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dash = await response.Content.ReadFromJsonAsync<JobApplicationDashboardDto>();
        dash.Should().NotBeNull();
        dash!.Total.Should().BeGreaterThanOrEqualTo(0);
    }

    private sealed record CreatedResponse(Guid Id);
}
