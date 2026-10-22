using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using JJDevHub.News.Infrastructure.Read;
using JJDevHub.Projections;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace JJDevHub.News.Api.Tests
{
    public sealed class ArticleEndpointsTests : IClassFixture<NewsApiFactory>
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly NewsApiFactory _factory;

        private readonly HttpClient _client;

        public ArticleEndpointsTests(NewsApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_without_token_returns_401()
        {
            HttpResponseMessage response = await PostAsync(token: null, title: "Nowy wpis", CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Post_returns_201_with_location()
        {
            string token = _factory.TokenFor(Guid.NewGuid());

            HttpResponseMessage response = await PostAsync(token, "Nowy wpis", CancellationToken.None);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var written = await response.Content.ReadFromJsonAsync<Written>(Json, CancellationToken.None);
            Assert.NotNull(written);
            Assert.EndsWith($"/api/articles/{written.ArticleId}", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Post_returns_etag_one()
        {
            string token = _factory.TokenFor(Guid.NewGuid());

            HttpResponseMessage response = await PostAsync(token, "Nowy wpis", CancellationToken.None);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
        }

        [Fact]
        public async Task Post_blank_title_returns_400()
        {
            string token = _factory.TokenFor(Guid.NewGuid());

            HttpResponseMessage response = await PostAsync(token, "  ", CancellationToken.None);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, CancellationToken.None);
            Assert.Equal(400, problem?.Status);
        }

        [Fact]
        public async Task Get_after_projection_returns_article()
        {
            var author = Guid.NewGuid();
            Written written = await CreateAsync(author, "Po projekcji", CancellationToken.None);
            await ProjectAsync(CancellationToken.None);

            HttpResponseMessage response = await _client.GetAsync(
                $"/api/articles/{written.ArticleId}",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var article = await response.Content.ReadFromJsonAsync<ArticleBody>(Json, CancellationToken.None);
            Assert.Equal("Po projekcji", article?.Title);
            Assert.Equal(author.ToString(), article?.AuthorIdentityGuid);
        }

        [Fact]
        public async Task Get_after_projection_returns_etag()
        {
            var author = Guid.NewGuid();
            Written written = await CreateAsync(author, "Po projekcji", CancellationToken.None);
            await ProjectAsync(CancellationToken.None);

            HttpResponseMessage response = await _client.GetAsync(
                $"/api/articles/{written.ArticleId}",
                CancellationToken.None);

            Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
        }

        [Fact]
        public async Task Get_unknown_returns_404()
        {
            HttpResponseMessage response = await _client.GetAsync(
                $"/api/articles/{Guid.NewGuid()}",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Put_without_if_match_returns_428()
        {
            var author = Guid.NewGuid();
            Written article = await CreateAsync(author, "Nowy wpis", CancellationToken.None);

            HttpResponseMessage response = await PutAsync(
                author,
                article.ArticleId,
                ifMatch: null,
                title: "Zmiana",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        }

        [Fact]
        public async Task Put_stale_if_match_returns_412()
        {
            var author = Guid.NewGuid();
            Written article = await CreateAsync(author, "Nowy wpis", CancellationToken.None);
            await AcceptRevisionAsync(author, article.ArticleId, "\"1\"", "Wersja druga", CancellationToken.None);

            HttpResponseMessage response = await PutAsync(
                author,
                article.ArticleId,
                "\"1\"",
                "Za stara",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
        }

        [Fact]
        public async Task Put_by_other_user_returns_403()
        {
            var author = Guid.NewGuid();
            Written article = await CreateAsync(author, "Nowy wpis", CancellationToken.None);

            HttpResponseMessage response = await PutAsync(
                Guid.NewGuid(),
                article.ArticleId,
                "\"1\"",
                "Cudza",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Put_returns_new_etag()
        {
            var author = Guid.NewGuid();
            Written article = await CreateAsync(author, "Nowy wpis", CancellationToken.None);

            HttpResponseMessage response = await PutAsync(
                author,
                article.ArticleId,
                "\"1\"",
                "Po rewizji",
                CancellationToken.None);

            Assert.Equal("\"2\"", response.Headers.ETag?.Tag);
        }

        [Fact]
        public async Task List_too_large_page_returns_400()
        {
            HttpResponseMessage response = await _client.GetAsync(
                "/api/articles?pageSize=51",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Request_without_version_uses_default()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/articles", CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("api-supported-versions", out IEnumerable<string>? versions));
            Assert.Contains("1.0", versions ?? []);
        }

        [Fact]
        public async Task Unknown_version_returns_400()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/articles");
            request.Headers.TryAddWithoutValidation("api-version", "9.0");

            HttpResponseMessage response = await _client.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, CancellationToken.None);
            Assert.Equal(400, problem?.Status);
        }

        private async Task ProjectAsync(CancellationToken cancellationToken)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<ProjectionRunner<NewsReadContext>>();
            await runner.RunOnceAsync(cancellationToken);
        }

        private async Task<Written> CreateAsync(Guid author, string title, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await PostAsync(_factory.TokenFor(author), title, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new InvalidOperationException("Article was not created.");
            }

            var written = await response.Content.ReadFromJsonAsync<Written>(Json, cancellationToken);
            if (written is null)
            {
                throw new InvalidOperationException("Article was not created.");
            }

            return written;
        }

        private async Task AcceptRevisionAsync(
            Guid author,
            Guid articleId,
            string ifMatch,
            string title,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await PutAsync(author, articleId, ifMatch, title, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Article was not revised.");
            }
        }

        private async Task<HttpResponseMessage> PostAsync(string? token, string title, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/articles");
            request.Content = JsonContent.Create(new { title, body = "treść" });
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await _client.SendAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> PutAsync(
            Guid author,
            Guid articleId,
            string? ifMatch,
            string title,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/articles/{articleId}");
            request.Content = JsonContent.Create(new { title, body = "treść" });
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(author));
            if (ifMatch is not null)
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.IfMatch, ifMatch);
            }

            return await _client.SendAsync(request, cancellationToken);
        }

        private sealed record Written(Guid ArticleId, int Version);

        private sealed record ArticleBody(Guid ArticleId, string Title, string? AuthorIdentityGuid);
    }
}
