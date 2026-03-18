using System.Net;
using System.Net.Http.Json;
using JJDevHub.Content.Api.Endpoints;
using JJDevHub.Content.Application.Commands.AddWorkExperience;
using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.IntegrationTests.Fixtures;
using JJDevHub.Content.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task UpdateWorkExperience_WithCorrectVersion_ShouldReturn204()
    {
        var command = new AddWorkExperienceCommand(
            "Updatable Co", "Dev",
            new DateTime(2022, 1, 1), null, true);
        var createResponse = await _client.PostAsJsonAsync("/api/content/work-experiences", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var getResponse = await _client.GetAsync($"/api/content/work-experiences/{created!.Id}");
        var dto = await getResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();

        var update = new UpdateWorkExperienceRequest(
            dto!.Version,
            "Updated Co",
            dto.Position,
            dto.StartDate,
            dto.EndDate,
            dto.IsPublic);

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/content/work-experiences/{dto.Id}",
            update);

        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateWorkExperience_WithStaleVersion_ShouldReturn409()
    {
        var command = new AddWorkExperienceCommand(
            "Stale Co", "Dev",
            new DateTime(2021, 1, 1), null, true);
        var createResponse = await _client.PostAsJsonAsync("/api/content/work-experiences", command);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var getResponse = await _client.GetAsync($"/api/content/work-experiences/{created!.Id}");
        var dto = await getResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();

        var updateOk = new UpdateWorkExperienceRequest(
            dto!.Version,
            "First update",
            dto.Position,
            dto.StartDate,
            dto.EndDate,
            dto.IsPublic);
        var firstPut = await _client.PutAsJsonAsync($"/api/content/work-experiences/{dto.Id}", updateOk);
        firstPut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stale = updateOk with { CompanyName = "Second update", Version = dto.Version };
        var putResponse = await _client.PutAsJsonAsync($"/api/content/work-experiences/{dto.Id}", stale);

        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWorkExperience_ShouldWriteOutboxRow()
    {
        var command = new AddWorkExperienceCommand(
            "Outbox Corp", "Engineer",
            new DateTime(2023, 1, 1), null, true);

        var response = await _client.PostAsJsonAsync("/api/content/work-experiences", command);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        var outboxRow = await db.OutboxMessages
            .FirstOrDefaultAsync(m => m.AggregateId == created!.Id);

        outboxRow.Should().NotBeNull();
        outboxRow!.EventType.Should().Be("WorkExperienceCreatedIntegrationEvent");
        outboxRow.ProcessedUtc.Should().BeNull("the publisher is disabled in tests");
    }

    private record CreatedResponse(Guid Id);
}
