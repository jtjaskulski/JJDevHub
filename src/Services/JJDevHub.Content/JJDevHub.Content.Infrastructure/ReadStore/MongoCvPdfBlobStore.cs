using JJDevHub.Content.Application.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoCvPdfBlobStore : ICvPdfBlobStore
{
    private readonly IMongoCollection<CvPdfBlobDocument> _collection;

    public MongoCvPdfBlobStore(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<CvPdfBlobDocument>("cv_pdf_blobs");
    }

    public async Task<Guid> SaveAsync(
        Guid curriculumVitaeId,
        Guid? jobApplicationId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var doc = new CvPdfBlobDocument
        {
            Id = id,
            CurriculumVitaeId = curriculumVitaeId,
            JobApplicationId = jobApplicationId,
            FileName = fileName,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return id;
    }

    public async Task<(byte[] Content, string FileName)?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<CvPdfBlobDocument>.Filter.Eq(d => d.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : (doc.Content, doc.FileName);
    }
}
