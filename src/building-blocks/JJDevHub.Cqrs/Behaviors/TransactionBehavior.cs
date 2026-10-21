using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace JJDevHub.Cqrs.Behaviors
{
    public sealed class TransactionBehavior<TRequest, TResponse>(IServiceProvider services)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not ICommand<TResponse>)
            {
                return await next();
            }

            var unitOfWork = services.GetService<IUnitOfWork>();
            if (unitOfWork is null || unitOfWork.HasActiveTransaction)
            {
                return await next();
            }

            await unitOfWork.BeginAsync(cancellationToken);
            bool completed = false;
            try
            {
                var result = await next();
                if (result.Succeeded)
                {
                    await unitOfWork.CommitAsync(cancellationToken);
                }
                else
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                }

                completed = true;
                return result;
            }
            finally
            {
                if (!completed)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                }
            }
        }
    }
}