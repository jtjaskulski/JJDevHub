using FluentValidation;

using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs.Tests
{

    public sealed class ValidatorBehaviorTests
    {
        [Fact]
        public async Task Invalid_request_skips_handler()
        {
            var gate = new HandlerGate();
            var services = TestServices.Create();
            services.AddSingleton(gate);
            services.AddSingleton<IValidator<NameRequest>, NameRequestValidator>();
            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new NameRequest(""), CancellationToken.None);

            Assert.Equal(ResultError.Invalid, result.Error);
            Assert.False(gate.Called);
        }

        [Fact]
        public async Task Valid_request_reaches_handler()
        {
            var gate = new HandlerGate();
            var services = TestServices.Create();
            services.AddSingleton(gate);
            services.AddSingleton<IValidator<NameRequest>, NameRequestValidator>();
            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new NameRequest("article"), CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.True(gate.Called);
        }

        [Fact]
        public async Task Validator_receives_cancellation_token()
        {
            var validator = new TokenRequestValidator();
            var services = TestServices.Create();
            services.AddSingleton<IValidator<TokenRequest>>(validator);
            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            using var source = new CancellationTokenSource();

            await mediator.Send(new TokenRequest("article"), source.Token);

            Assert.Equal(source.Token, validator.Token);
        }
    }

    internal sealed class HandlerGate
    {
        public bool Called { get; set; }
    }

    internal sealed record NameRequest(string Name) : ICommand<int>;

    internal sealed class NameRequestValidator : AbstractValidator<NameRequest>
    {
        public NameRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
        }
    }

    internal sealed class NameRequestHandler(HandlerGate gate) : IRequestHandler<NameRequest, int>
    {
        public Task<Result<int>> Handle(NameRequest request, CancellationToken cancellationToken)
        {
            gate.Called = true;
            return Task.FromResult(Result<int>.Ok(1));
        }
    }

    internal sealed record TokenRequest(string Name) : ICommand<int>;

    internal sealed class TokenRequestValidator : AbstractValidator<TokenRequest>
    {
        public CancellationToken Token { get; private set; }

        public TokenRequestValidator()
        {
            RuleFor(request => request.Name).MustAsync(Remember);
        }

        private Task<bool> Remember(string name, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return Task.FromResult(true);
        }
    }

    internal sealed class TokenRequestHandler : IRequestHandler<TokenRequest, int>
    {
        public Task<Result<int>> Handle(TokenRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Ok(1));
        }
    }
}
