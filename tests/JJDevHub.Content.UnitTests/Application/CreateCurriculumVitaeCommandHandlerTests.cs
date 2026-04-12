using JJDevHub.Content.Application.Commands.CreateCurriculumVitae;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using NSubstitute;

namespace JJDevHub.Content.UnitTests.Application;

public class CreateCurriculumVitaeCommandHandlerTests
{
    private readonly ICurriculumVitaeRepository _repository;
    private readonly CreateCurriculumVitaeCommandHandler _handler;

    public CreateCurriculumVitaeCommandHandlerTests()
    {
        _repository = Substitute.For<ICurriculumVitaeRepository>();
        _repository.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        _handler = new CreateCurriculumVitaeCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldAddAndSave()
    {
        var command = new CreateCurriculumVitaeCommand(
            "Jan", "Kowalski", "jan@example.com", null, null, null);

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<CurriculumVitae>(), Arg.Any<CancellationToken>());
        await _repository.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
