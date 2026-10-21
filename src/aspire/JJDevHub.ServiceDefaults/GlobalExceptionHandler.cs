using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JJDevHub.ServiceDefaults
{

    public sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogDebug(exception, "Request aborted on {Path}", httpContext.Request.Path);
                return true;
            }

            logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
            Activity.Current?.AddException(exception);
            Activity.Current?.SetStatus(ActivityStatusCode.Error);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails =
                {
                    Title = "Unexpected error",
                    Status = StatusCodes.Status500InternalServerError,
                    Extensions =
                    {
                        ["traceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
                    },
                },
            });
        }
    }
}
