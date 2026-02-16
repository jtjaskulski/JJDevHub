using JJDevHub.Content.Application.Commands.DeleteWorkExperience;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.UnitTests.Application;

public class DeleteWorkExperienceCommandHandlerTests
{
    private readonly IWorkExperienceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteWorkExperienceCommandHandler _handler;

    public DeleteWorkExperienceCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _repository = Substitute.For<IWorkExperienceRepository>();
        _repository.UnitOfWork.Returns(_unitOfWork);
        _handler = new DeleteWorkExperienceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithExistingExperience_ShouldDeleteAndSave()
    {
        var experience = WorkExperience.Create(
            "Corp", "Dev", new DateTime(2023, 1, 1), null, true);
        _repository.GetByIdAsync(experience.Id, Arg.Any<CancellationToken>())
            .Returns(experience);

        var command = new DeleteWorkExperienceCommand(experience.Id);
        await _handler.Handle(command, CancellationToken.None);

        _repository.Received(1).Delete(experience);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingExperience_ShouldThrowKeyNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkExperience?)null);

        var command = new DeleteWorkExperienceCommand(Guid.NewGuid());
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
