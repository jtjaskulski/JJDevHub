using System.Collections.Concurrent;

using JJDevHub.Domain;

namespace JJDevHub.Cqrs
{

    public sealed class Mediator(IServiceProvider services) : IMediator
    {
        private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

        public Task<Result<TResponse>> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            var requestType = request.GetType();
            var cached = Wrappers.GetOrAdd(requestType, CreateWrapper, typeof(TResponse));
            if (cached is not RequestHandlerWrapper<TResponse> wrapper)
            {
                throw new InvalidOperationException(
                    $"Handler wrapper for '{requestType.Name}' does not return '{typeof(TResponse).Name}'.");
            }

            return wrapper.Handle(request, services, cancellationToken);
        }

        private static object CreateWrapper(Type requestType, Type responseType)
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
            var instance = Activator.CreateInstance(wrapperType);
            if (instance is null)
            {
                throw new InvalidOperationException(
                    $"Could not create a handler wrapper for request '{requestType.Name}'.");
            }

            return instance;
        }
    }
}
