using System.Net;
using System.Net.Http.Json;
using JJDevHub.Content.Application.Commands.AddWorkExperience;
using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.IntegrationTests.Fixtures;

namespace JJDevHub.Content.IntegrationTests.Api;

public class WorkExperienceEndpointsTests : IClassFixture<ContentApiFactory>, IAsyncLifetime
{
    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public WorkExperienceEndpointsTests(ContentApiFactory factory)
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
    public async Task CreateWorkExperience_ShouldReturn201()
    {
        var command = new AddWorkExperienceCommand(
            "Integration Corp", "Test Engineer",
            new DateTime(2023, 1, 1), null, true);

        var response = await _client.PostAsJsonAsync("/api/content/work-experiences", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkExperiences_AfterCreate_ShouldReturnList()
    {
        var command = new AddWorkExperienceCommand(
            "TestCo", "Dev",
            new DateTime(2023, 6, 1), null, true);
        await _client.PostAsJsonAsync("/api/content/work-experiences", command);

        var response = await _client.GetAsync("/api/content/work-experiences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var experiences = await response.Content.ReadFromJsonAsync<List<WorkExperienceDto>>();
        experiences.Should().NotBeEmpty();
        experiences!.Should().Contain(e => e.CompanyName == "TestCo");
    }

    [Fact]
    public async Task GetWorkExperienceById_WithExistingId_ShouldReturn200()
    {
        var command = new AddWorkExperienceCommand(
            "FindMe Corp", "Architect",
            new DateTime(2022, 3, 1), new DateTime(2024, 1, 1), true);
        var createResponse = await _client.PostAsJsonAsync("/api/content/work-experiences", command);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await _client.GetAsync($"/api/content/work-experiences/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<WorkExperienceDto>();
        dto!.CompanyName.Should().Be("FindMe Corp");
        dto.Position.Should().Be("Architect");
    }

    [Fact]
    public async Task GetWorkExperienceById_WithNonExistingId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/content/work-experiences/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWorkExperience_WithExistingId_ShouldReturn204()
    {
        var command = new AddWorkExperienceCommand(
            "DeleteMe", "Temp",
            new DateTime(2023, 1, 1), null, true);
        var createResponse = await _client.PostAsJsonAsync("/api/content/work-experiences", command);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await _client.DeleteAsync($"/api/content/work-experiences/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateWorkExperience_WithInvalidData_ShouldReturn400()
    {
        var command = new AddWorkExperienceCommand(
            "", "",
            new DateTime(2023, 1, 1), null, true);

        var response = await _client.PostAsJsonAsync("/api/content/work-experiences", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkExperiences_PublicOnly_ShouldFilterCorrectly()
    {
        await _client.PostAsJsonAsync("/api/content/work-experiences",
            new AddWorkExperienceCommand("Public Corp", "Dev", new DateTime(2023, 1, 1), null, true));
        await _client.PostAsJsonAsync("/api/content/work-experiences",
            new AddWorkExperienceCommand("Private Corp", "Dev", new DateTime(2023, 1, 1), null, false));

        var response = await _client.GetAsync("/api/content/work-experiences?publicOnly=true");

        var experiences = await response.Content.ReadFromJsonAsync<List<WorkExperienceDto>>();
        experiences!.Should().AllSatisfy(e => e.IsPublic.Should().BeTrue());
        experiences.Should().Contain(e => e.CompanyName == "Public Corp");
        experiences.Should().NotContain(e => e.CompanyName == "Private Corp");
    }

    private record CreatedResponse(Guid Id);
}
