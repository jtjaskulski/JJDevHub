using System.Net;
using System.Net.Http.Json;
using JJDevHub.Content.Api.Endpoints;
using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.IntegrationTests.Fixtures;
using JJDevHub.Content.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Content.IntegrationTests.Api;

public class CurriculumVitaeEndpointsTests : IClassFixture<ContentApiFactory>, IAsyncLifetime
{
    private const string BasePath = "/api/v1/content/cv";

    private readonly ContentApiFactory _factory;
    private readonly HttpClient _client;

    public CurriculumVitaeEndpointsTests(ContentApiFactory factory)
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
    public async Task CreateCurriculumVitae_ShouldReturn201()
    {
        var request = new CreateCurriculumVitaeRequest(
            "Jan",
            "Kowalski",
            "jan@example.com",
            "+48111222333",
            "Warsaw",
            "Bio");

        var response = await _client.PostAsJsonAsync(BasePath, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurriculumVitaeById_AfterCreate_ShouldReturn200()
    {
        var create = new CreateCurriculumVitaeRequest(
            "A", "B", "a@b.com", null, null, null);
        var createResponse = await _client.PostAsJsonAsync(BasePath, create);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await _client.GetAsync($"{BasePath}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CurriculumVitaeDto>();
        dto!.PersonalInfo.Email.Should().Be("a@b.com");
    }

    [Fact]
    public async Task GetCurriculumVitaes_AfterCreate_ShouldReturnList()
    {
        await _client.PostAsJsonAsync(
            BasePath,
            new CreateCurriculumVitaeRequest("X", "Y", "x@y.com", null, null, null));

        var response = await _client.GetAsync($"{BasePath}/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<CurriculumVitaeDto>>();
        list.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteCurriculumVitae_ShouldReturn204()
    {
        var createResponse = await _client.PostAsJsonAsync(
            BasePath,
            new CreateCurriculumVitaeRequest("Del", "Me", "del@me.com", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await _client.DeleteAsync($"{BasePath}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateCurriculumVitae_ShouldWriteOutboxRow()
    {
        var response = await _client.PostAsJsonAsync(
            BasePath,
            new CreateCurriculumVitaeRequest("Out", "Box", "out@box.com", null, null, null));
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        var outboxRow = await db.OutboxMessages
            .FirstOrDefaultAsync(m => m.AggregateId == created!.Id);

        outboxRow.Should().NotBeNull();
        outboxRow!.EventType.Should().Be("CurriculumVitaeCreatedIntegrationEvent");
    }

    private record CreatedResponse(Guid Id);
}
