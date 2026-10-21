using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs.Tests
{
    public sealed class TransactionBehaviorTests
    {
        [Fact]
        public async Task Successful_command_commits()
        {
            var unitOfWork = new FakeUnitOfWork(active: false);
            await using var provider = Create(unitOfWork);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new CommitCommand(), CancellationToken.None);

            Assert.Equal(["Begin", "Commit"], unitOfWork.Calls);
        }

        [Fact]
        public async Task Failed_command_rolls_back()
        {
            var unitOfWork = new FakeUnitOfWork(active: false);
            await using var provider = Create(unitOfWork);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new FailCommand(), CancellationToken.None);

            Assert.Equal(["Begin", "Rollback"], unitOfWork.Calls);
        }

        [Fact]
        public async Task Throwing_command_rolls_back()
        {
            var unitOfWork = new FakeUnitOfWork(active: false);
            await using var provider = Create(unitOfWork);
            var mediator = provider.GetRequiredService<IMediator>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                mediator.Send(new ThrowCommand(), CancellationToken.None));

            Assert.Equal(["Begin", "Rollback"], unitOfWork.Calls);
        }

        [Fact]
        public async Task Query_skips_transaction()
        {
            var unitOfWork = new FakeUnitOfWork(active: false);
            await using var provider = Create(unitOfWork);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new SkipQuery(), CancellationToken.None);

            Assert.DoesNotContain("Begin", unitOfWork.Calls);
        }

        [Fact]
        public async Task Active_transaction_is_reused()
        {
            var unitOfWork = new FakeUnitOfWork(active: true);
            await using var provider = Create(unitOfWork);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new ActiveCommand(), CancellationToken.None);

            Assert.DoesNotContain("Begin", unitOfWork.Calls);
        }

        private static ServiceProvider Create(FakeUnitOfWork unitOfWork)
        {
            var services = TestServices.Create();
            services.AddSingleton<IUnitOfWork>(unitOfWork);
            return services.BuildServiceProvider();
        }
    }

    internal sealed class FakeUnitOfWork(bool active) : IUnitOfWork
    {
        public List<string> Calls { get; } = [];

        public bool HasActiveTransaction { get; } = active;

        public Task BeginAsync(CancellationToken cancellationToken)
        {
            Calls.Add("Begin");
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Calls.Add("Commit");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            Calls.Add("Rollback");
            return Task.CompletedTask;
        }
    }

    internal sealed record CommitCommand : ICommand<int>;

    internal sealed class CommitCommandHandler : IRequestHandler<CommitCommand, int>
    {
        public Task<Result<int>> Handle(CommitCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Ok(1));
        }
    }

    internal sealed record FailCommand : ICommand<int>;

    internal sealed class FailCommandHandler : IRequestHandler<FailCommand, int>
    {
        public Task<Result<int>> Handle(FailCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Fail(ResultError.Conflict));
        }
    }

    internal sealed record ThrowCommand : ICommand<int>;

    internal sealed class ThrowCommandHandler : IRequestHandler<ThrowCommand, int>
    {
        public Task<Result<int>> Handle(ThrowCommand request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    internal sealed record SkipQuery : IQuery<int>;

    internal sealed class SkipQueryHandler : IRequestHandler<SkipQuery, int>
    {
        public Task<Result<int>> Handle(SkipQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Ok(1));
        }
    }

    internal sealed record ActiveCommand : ICommand<int>;

    internal sealed class ActiveCommandHandler : IRequestHandler<ActiveCommand, int>
    {
        public Task<Result<int>> Handle(ActiveCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Ok(1));
        }
    }
}