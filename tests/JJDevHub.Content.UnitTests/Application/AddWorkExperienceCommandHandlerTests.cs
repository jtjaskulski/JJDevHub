using JJDevHub.Content.Application.Commands.AddWorkExperience;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Content.Core.Repositories;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.UnitTests.Application;

public class AddWorkExperienceCommandHandlerTests
{
    private readonly IWorkExperienceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddWorkExperienceCommandHandler _handler;

    public AddWorkExperienceCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _repository = Substitute.For<IWorkExperienceRepository>();
        _repository.UnitOfWork.Returns(_unitOfWork);
        _handler = new AddWorkExperienceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddAndSave()
    {
        var command = new AddWorkExperienceCommand(
            "Microsoft", "Developer",
            new DateTime(2023, 1, 1), null, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkExperience>(e =>
                e.CompanyName == "Microsoft" &&
                e.Position == "Developer" &&
                e.IsPublic),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNewGuid()
    {
        var command = new AddWorkExperienceCommand(
            "Google", "SRE",
            new DateTime(2022, 6, 1), new DateTime(2024, 1, 1), false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }
}
