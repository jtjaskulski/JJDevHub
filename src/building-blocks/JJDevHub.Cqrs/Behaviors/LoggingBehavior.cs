using System.Diagnostics;

using JJDevHub.Cqrs;
using JJDevHub.Domain;

using Microsoft.Extensions.Logging;

namespace JJDevHub.Cqrs.Behaviors
{

    public sealed class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogInformation("Handling {RequestName}", requestName);

            var stopwatch = Stopwatch.StartNew();
            using var activity = CqrsTelemetry.ActivitySource.StartActivity($"Send {requestName}");
            var result = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs} ms with {Error}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                result.Error);
            return result;
        }
    }
}
