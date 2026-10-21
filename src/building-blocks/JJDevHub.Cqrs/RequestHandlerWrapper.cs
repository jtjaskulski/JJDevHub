using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs
{
    internal abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> Handle(
            IRequest<TResponse> request,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<Result<TResponse>> Handle(
            IRequest<TResponse> request,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            if (request is not TRequest typed)
            {
                throw new InvalidOperationException(
                    $"Request '{request.GetType().Name}' is not '{typeof(TRequest).Name}'.");
            }

            var handler = services.GetService<IRequestHandler<TRequest, TResponse>>();
            if (handler is null)
            {
                throw new InvalidOperationException(
                    $"No handler registered for request '{typeof(TRequest).Name}'.");
            }

            RequestHandlerDelegate<TResponse> next = () => handler.Handle(typed, cancellationToken);
            foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var current = behavior;
                var nextCopy = next;
                next = () => current.Handle(typed, nextCopy, cancellationToken);
            }

            return next();
        }
    }
}