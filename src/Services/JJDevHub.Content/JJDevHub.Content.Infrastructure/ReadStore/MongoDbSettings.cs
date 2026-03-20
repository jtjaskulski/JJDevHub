namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = "mongodb://localhost:27018";
    public string DatabaseName { get; init; } = "jjdevhub_content_read";
}
