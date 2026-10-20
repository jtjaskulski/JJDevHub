using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs.Tests
{

    public sealed class MediatorTests
    {
        [Fact]
        public async Task Send_reaches_handler()
        {
            await using var provider = TestServices.Create().BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new ReachCommand(), CancellationToken.None);

            Assert.True(result.TryGetValue(out var value));
            Assert.Equal(42, value);
        }

        [Fact]
        public async Task Send_without_handler_throws()
        {
            await using var provider = TestServices.Create().BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                mediator.Send(new MissingCommand(), CancellationToken.None));

            Assert.Contains(nameof(MissingCommand), exception.Message);
        }

        [Fact]
        public async Task Behaviors_wrap_in_registration_order()
        {
            var trace = new List<string>();
            var services = TestServices.Create();
            services.AddSingleton(trace);
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new OrderCommand(), CancellationToken.None);

            Assert.Equal(["a>", "b>", "handler", "<b", "<a"], trace);
        }

        [Fact]
        public async Task Send_passes_cancellation_token()
        {
            var seen = new SeenToken();
            var services = TestServices.Create();
            services.AddSingleton(seen);
            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            using var source = new CancellationTokenSource();

            await mediator.Send(new SeenTokenCommand(), source.Token);

            Assert.Equal(source.Token, seen.Token);
        }
    }

    internal sealed record ReachCommand : ICommand<int>;

    internal sealed class ReachCommandHandler : IRequestHandler<ReachCommand, int>
    {
        public Task<Result<int>> Handle(ReachCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Ok(42));
        }
    }

    internal sealed record MissingCommand : ICommand<int>;

    internal sealed record OrderCommand : ICommand<int>;

    internal sealed class OrderCommandHandler(List<string> trace) : IRequestHandler<OrderCommand, int>
    {
        public Task<Result<int>> Handle(OrderCommand request, CancellationToken cancellationToken)
        {
            trace.Add("handler");
            return Task.FromResult(Result<int>.Ok(1));
        }
    }

    internal sealed class OuterBehavior<TRequest, TResponse>(List<string> trace) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            trace.Add("a>");
            var result = await next();
            trace.Add("<a");
            return result;
        }
    }

    internal sealed class InnerBehavior<TRequest, TResponse>(List<string> trace) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            trace.Add("b>");
            var result = await next();
            trace.Add("<b");
            return result;
        }
    }

    internal sealed class SeenToken
    {
        public CancellationToken Token { get; set; }
    }

    internal sealed record SeenTokenCommand : ICommand<int>;

    internal sealed class SeenTokenCommandHandler(SeenToken seen) : IRequestHandler<SeenTokenCommand, int>
    {
        public Task<Result<int>> Handle(SeenTokenCommand request, CancellationToken cancellationToken)
        {
            seen.Token = cancellationToken;
            return Task.FromResult(Result<int>.Ok(1));
        }
    }
}
