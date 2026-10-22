using JJDevHub.Domain;

namespace JJDevHub.News.Api.Http
{
    public static class ResultHttpExtensions
    {
        public static IResult ToProblem(this ResultError error) =>
            error switch
            {
                ResultError.Invalid => Problem(StatusCodes.Status400BadRequest, "The request is invalid."),
                ResultError.NotFound => Problem(StatusCodes.Status404NotFound, "Not found."),
                ResultError.Conflict => Problem(StatusCodes.Status409Conflict, "Conflict."),
                ResultError.Forbidden => Problem(StatusCodes.Status403Forbidden, "Forbidden."),
                _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected error."),
            };

        private static IResult Problem(int status, string title) =>
            Results.Problem(statusCode: status, title: title);
    }
}
