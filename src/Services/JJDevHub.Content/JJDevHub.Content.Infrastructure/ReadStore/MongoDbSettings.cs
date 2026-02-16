namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "jjdevhub_content_read";
}
