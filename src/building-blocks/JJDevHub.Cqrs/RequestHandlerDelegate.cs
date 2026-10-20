using JJDevHub.Domain;

namespace JJDevHub.Cqrs
{

    public delegate Task<Result<TResponse>> RequestHandlerDelegate<TResponse>();
}
