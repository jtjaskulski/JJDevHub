using System.Net;

namespace JJDevHub.Gateway.Versions
{
    public sealed record VersionReport(
        string Identity,
        string News,
        string Learning,
        string Cv,
        string Contact,
        string Gateway)
    {
        public const string HttpClientName = "versions";

        private static readonly TimeSpan NeighborTimeout = TimeSpan.FromSeconds(2);

        public static async Task<VersionReport> CollectAsync(
            HttpClient httpClient,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            Task<string> identity = ReadAsync(httpClient, configuration, "identity", cancellationToken);
            Task<string> news = ReadAsync(httpClient, configuration, "news", cancellationToken);
            Task<string> learning = ReadAsync(httpClient, configuration, "learning", cancellationToken);
            Task<string> cv = ReadAsync(httpClient, configuration, "cv", cancellationToken);
            Task<string> contact = ReadAsync(httpClient, configuration, "contact", cancellationToken);

            await Task.WhenAll(identity, news, learning, cv, contact);

            string gateway = configuration["SERVICE_VERSION"] ?? "";

            return new VersionReport(
                await identity,
                await news,
                await learning,
                await cv,
                await contact,
                gateway);
        }

        private static async Task<string> ReadAsync(
            HttpClient httpClient,
            IConfiguration configuration,
            string cluster,
            CancellationToken cancellationToken)
        {
            string? address = configuration[$"ReverseProxy:Clusters:{cluster}:Destinations:destination:Address"];
            if (string.IsNullOrWhiteSpace(address))
            {
                return "";
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(NeighborTimeout);

            try
            {
                var uri = new Uri($"{address.TrimEnd('/')}/version");
                using HttpResponseMessage response = await httpClient.GetAsync(uri, timeout.Token);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return "";
                }

                string body = await response.Content.ReadAsStringAsync(timeout.Token);
                return body.Trim();
            }
            catch (HttpRequestException)
            {
                return "";
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return "";
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }
    }
}