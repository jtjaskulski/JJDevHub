using JJDevHub.Domain;

namespace JJDevHub.Cqrs
{
    public interface IMediator
    {
        Task<Result<TResponse>> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken);
    }
}