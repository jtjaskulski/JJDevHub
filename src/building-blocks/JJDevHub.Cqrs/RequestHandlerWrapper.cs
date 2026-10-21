using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs
{
    public abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> Handle(
            IRequest<TResponse> request,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    public sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
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
                    $"Handler for '{typeof(TRequest).Name}' is not registered.");
            }

            return Invoke(typed, handler, services, cancellationToken);
        }

        private static Task<Result<TResponse>> Invoke(
            TRequest request,
            IRequestHandler<TRequest, TResponse> handler,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();
            RequestHandlerDelegate<TResponse> next = () => handler.Handle(request, cancellationToken);
            for (var index = behaviors.Count - 1; index >= 0; index--)
            {
                next = Wrap(behaviors[index], request, next, cancellationToken);
            }

            return next();
        }

        private static RequestHandlerDelegate<TResponse> Wrap(
            IPipelineBehavior<TRequest, TResponse> behavior,
            TRequest request,
            RequestHandlerDelegate<TResponse> inner,
            CancellationToken cancellationToken) =>
            () => behavior.Handle(request, inner, cancellationToken);
    }
}