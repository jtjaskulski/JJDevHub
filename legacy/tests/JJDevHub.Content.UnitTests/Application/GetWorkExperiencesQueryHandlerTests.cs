using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.Queries.GetWorkExperiences;

namespace JJDevHub.Content.UnitTests.Application;

public class GetWorkExperiencesQueryHandlerTests
{
    private readonly IWorkExperienceReadStore _readStore;
    private readonly GetWorkExperiencesQueryHandler _handler;

    public GetWorkExperiencesQueryHandlerTests()
    {
        _readStore = Substitute.For<IWorkExperienceReadStore>();
        _handler = new GetWorkExperiencesQueryHandler(_readStore);
    }

    [Fact]
    public async Task Handle_PublicOnlyFalse_ShouldCallGetAll()
    {
        var expected = new List<WorkExperienceDto>
        {
            new(Guid.NewGuid(), 1L, "Corp1", "Dev", DateTime.UtcNow.AddYears(-2), null, true, true, 24),
            new(Guid.NewGuid(), 1L, "Corp2", "QA", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, false, false, 12)
        }.AsReadOnly();
        _readStore.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var query = new GetWorkExperiencesQuery(PublicOnly: false);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        await _readStore.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _readStore.DidNotReceive().GetPublicAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublicOnlyTrue_ShouldCallGetPublic()
    {
        var expected = new List<WorkExperienceDto>
        {
            new(Guid.NewGuid(), 1L, "Corp1", "Dev", DateTime.UtcNow.AddYears(-2), null, true, true, 24)
        }.AsReadOnly();
        _readStore.GetPublicAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var query = new GetWorkExperiencesQuery(PublicOnly: true);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        await _readStore.Received(1).GetPublicAsync(Arg.Any<CancellationToken>());
    }
}
