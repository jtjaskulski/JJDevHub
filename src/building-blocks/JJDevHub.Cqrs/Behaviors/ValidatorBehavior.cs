using FluentValidation;

using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.Logging;

namespace JJDevHub.Cqrs.Behaviors
{
    public sealed class ValidatorBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidatorBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
            {
                return await next();
            }

            bool failed = false;
            foreach (var validator in validators)
            {
                var validation = await validator.ValidateAsync(request, cancellationToken);
                if (validation.IsValid)
                {
                    continue;
                }

                failed = true;
                foreach (var failure in validation.Errors)
                {
                    logger.LogDebug(
                        "Validation failed for {RequestName} on {PropertyName} with {ErrorCode}",
                        typeof(TRequest).Name,
                        failure.PropertyName,
                        failure.ErrorCode);
                }
            }

            if (failed)
            {
                return Result<TResponse>.Fail(ResultError.Invalid);
            }

            return await next();
        }
    }
}