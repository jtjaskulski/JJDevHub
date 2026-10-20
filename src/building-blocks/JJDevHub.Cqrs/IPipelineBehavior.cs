using JJDevHub.Domain;

namespace JJDevHub.Cqrs
{

    public interface IPipelineBehavior<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }
}
