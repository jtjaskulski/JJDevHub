using Asp.Versioning;
using Asp.Versioning.Builder;

using JJDevHub.Cqrs;
using JJDevHub.Domain;
using JJDevHub.News.Api.Http;
using JJDevHub.News.Application.Articles;

namespace JJDevHub.News.Api.Articles
{
    public static class ArticleEndpoints
    {
        public static RouteGroupBuilder MapArticles(this WebApplication app)
        {
            var versions = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build();
            RouteGroupBuilder group = app.MapGroup("/api/articles")
                .WithApiVersionSet(versions)
                .WithTags("Articles");
            group.MapGet("", ListAsync);
            group.MapGet("/{id:guid}", GetAsync);
            group.MapPost("", CreateAsync).RequireAuthorization();
            group.MapPut("/{id:guid}", ReviseAsync).RequireAuthorization();
            return group;
        }

        private static async Task<IResult> ListAsync(
            IMediator mediator,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20)
        {
            Result<IReadOnlyList<ArticleSummary>> result = await mediator.Send(
                new ListArticlesQuery(page, pageSize),
                cancellationToken);
            if (!result.TryGetValue(out IReadOnlyList<ArticleSummary>? articles))
            {
                return result.Error.ToProblem();
            }

            return Results.Ok(articles);
        }

        private static async Task<IResult> GetAsync(
            Guid id,
            HttpContext http,
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            Result<ArticleSummary> result = await mediator.Send(new GetArticleQuery(id), cancellationToken);
            if (!result.TryGetValue(out ArticleSummary? article))
            {
                return result.Error.ToProblem();
            }

            http.Response.Headers.ETag = EntityTag.From(article.Version);
            return Results.Ok(article);
        }

        private static async Task<IResult> CreateAsync(
            CreateArticleRequest request,
            HttpContext http,
            IMediator mediator,
            CurrentUser currentUser,
            CancellationToken cancellationToken)
        {
            Result<ArticleWritten> result = await mediator.Send(
                new CreateArticleCommand(request.Title, request.Body, currentUser.IdentityGuid),
                cancellationToken);
            if (!result.TryGetValue(out ArticleWritten? written))
            {
                return result.Error.ToProblem();
            }

            http.Response.Headers.ETag = EntityTag.From(written.Version);
            return Results.Created($"/api/articles/{written.ArticleId}", written);
        }

        private static async Task<IResult> ReviseAsync(
            Guid id,
            ReviseArticleRequest request,
            HttpContext http,
            IMediator mediator,
            CurrentUser currentUser,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(http.Request.Headers.IfMatch))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status428PreconditionRequired,
                    title: "Precondition Required");
            }

            if (!EntityTag.TryParse(http.Request.Headers.IfMatch, out int version))
            {
                return ResultError.Invalid.ToProblem();
            }

            Result<ArticleWritten> result = await mediator.Send(
                new ReviseArticleCommand(id, version, request.Title, request.Body, currentUser.IdentityGuid),
                cancellationToken);
            if (!result.TryGetValue(out ArticleWritten? written))
            {
                return RevisionProblem(result.Error);
            }

            http.Response.Headers.ETag = EntityTag.From(written.Version);
            return Results.Ok(written);
        }

        private static IResult RevisionProblem(ResultError error)
        {
            if (error == ResultError.Conflict)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status412PreconditionFailed,
                    title: "Precondition Failed");
            }

            return error.ToProblem();
        }
    }
}
