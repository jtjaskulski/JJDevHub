using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoWorkExperienceReadStore : IWorkExperienceReadStore
{
    private readonly IMongoCollection<WorkExperienceDocument> _collection;

    public MongoWorkExperienceReadStore(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<WorkExperienceDocument>("work_experiences");
    }

    public async Task<WorkExperienceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.Id, id);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is not null ? MapToDto(document) : null;
    }

    public async Task<IReadOnlyList<WorkExperienceDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sort = Builders<WorkExperienceDocument>.Sort.Descending(d => d.StartDate);
        var documents = await _collection.Find(_ => true).Sort(sort).ToListAsync(cancellationToken);
        return documents.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkExperienceDto>> GetPublicAsync(CancellationToken cancellationToken)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.IsPublic, true);
        var sort = Builders<WorkExperienceDocument>.Sort.Descending(d => d.StartDate);
        var documents = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
        return documents.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task UpsertAsync(WorkExperienceReadModel model, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.Id, model.Id);

        var document = new WorkExperienceDocument
        {
            Id = model.Id,
            CompanyName = model.CompanyName,
            Position = model.Position,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsPublic = model.IsPublic,
            IsCurrent = model.IsCurrent,
            DurationInMonths = model.DurationInMonths,
            LastModifiedAt = model.LastModifiedAt
        };

        await _collection.ReplaceOneAsync(
            filter,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkExperienceDocument>.Filter.Eq(d => d.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    private static WorkExperienceDto MapToDto(WorkExperienceDocument document) => new(
        document.Id,
        document.CompanyName,
        document.Position,
        document.StartDate,
        document.EndDate,
        document.IsPublic,
        document.IsCurrent,
        document.DurationInMonths);
}
