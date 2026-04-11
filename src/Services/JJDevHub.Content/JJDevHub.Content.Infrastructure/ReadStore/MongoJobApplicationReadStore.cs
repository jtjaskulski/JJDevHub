using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoJobApplicationReadStore : IJobApplicationReadStore
{
    private readonly IMongoCollection<JobApplicationDocument> _collection;

    public MongoJobApplicationReadStore(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<JobApplicationDocument>("job_applications");
    }

    public async Task<JobApplicationReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is not null ? Map(doc) : null;
    }

    public async Task<IReadOnlyList<JobApplicationReadModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sort = Builders<JobApplicationDocument>.Sort.Descending(d => d.AppliedDate);
        var docs = await _collection.Find(_ => true).Sort(sort).ToListAsync(cancellationToken);
        return docs.Select(Map).ToList();
    }

    public async Task UpsertAsync(JobApplicationReadModel model, CancellationToken cancellationToken)
    {
        var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, model.Id);
        var document = new JobApplicationDocument
        {
            Id = model.Id,
            RowVersion = model.Version,
            CompanyName = model.CompanyName,
            Location = model.Location,
            WebsiteUrl = model.WebsiteUrl,
            Industry = model.Industry,
            Position = model.Position,
            Status = model.Status,
            AppliedDate = model.AppliedDate,
            LinkedCurriculumVitaeId = model.LinkedCurriculumVitaeId,
            Requirements = model.Requirements.Select(r => new JobApplicationRequirementDoc
            {
                Id = r.Id,
                Description = r.Description,
                Category = r.Category,
                Priority = r.Priority,
                IsMet = r.IsMet
            }).ToList(),
            Notes = model.Notes.Select(n => new JobApplicationNoteDoc
            {
                Id = n.Id,
                Content = n.Content,
                CreatedAt = n.CreatedAt,
                NoteType = n.NoteType
            }).ToList(),
            InterviewStages = model.InterviewStages.Select(s => new JobApplicationInterviewStageDoc
            {
                Id = s.Id,
                StageName = s.StageName,
                ScheduledAt = s.ScheduledAt,
                Status = s.Status,
                Feedback = s.Feedback
            }).ToList(),
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate,
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
        var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    private static JobApplicationReadModel Map(JobApplicationDocument d) => new()
    {
        Id = d.Id,
        Version = d.RowVersion > 0 ? d.RowVersion : 1,
        CompanyName = d.CompanyName,
        Location = d.Location,
        WebsiteUrl = d.WebsiteUrl,
        Industry = d.Industry,
        Position = d.Position,
        Status = d.Status,
        AppliedDate = d.AppliedDate,
        LinkedCurriculumVitaeId = d.LinkedCurriculumVitaeId,
        Requirements = d.Requirements.Select(r => new JobApplicationRequirementReadModel(
            r.Id,
            r.Description,
            r.Category,
            r.Priority,
            r.IsMet)).ToList(),
        Notes = d.Notes.Select(n => new JobApplicationNoteReadModel(
            n.Id,
            n.Content,
            n.CreatedAt,
            n.NoteType)).ToList(),
        InterviewStages = d.InterviewStages.Select(s => new JobApplicationInterviewStageReadModel(
            s.Id,
            s.StageName,
            s.ScheduledAt,
            s.Status,
            s.Feedback)).ToList(),
        CreatedDate = d.CreatedDate == default ? d.LastModifiedAt : d.CreatedDate,
        ModifiedDate = d.ModifiedDate,
        LastModifiedAt = d.LastModifiedAt
    };
}
