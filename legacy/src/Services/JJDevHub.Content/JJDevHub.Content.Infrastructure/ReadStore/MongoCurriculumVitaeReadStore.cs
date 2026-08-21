using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.Interfaces;
using JJDevHub.Content.Application.ReadModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JJDevHub.Content.Infrastructure.ReadStore;

public class MongoCurriculumVitaeReadStore : ICurriculumVitaeReadStore
{
    private readonly IMongoCollection<CurriculumVitaeDocument> _collection;

    public MongoCurriculumVitaeReadStore(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<CurriculumVitaeDocument>("curriculum_vitae");
    }

    public async Task<CurriculumVitaeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<CurriculumVitaeDocument>.Filter.Eq(d => d.Id, id);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is not null ? MapToDto(document) : null;
    }

    public async Task<IReadOnlyList<CurriculumVitaeDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sort = Builders<CurriculumVitaeDocument>.Sort.Descending(d => d.CreatedDate);
        var documents = await _collection.Find(_ => true).Sort(sort).ToListAsync(cancellationToken);
        return documents.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task UpsertAsync(CurriculumVitaeReadModel model, CancellationToken cancellationToken)
    {
        var filter = Builders<CurriculumVitaeDocument>.Filter.Eq(d => d.Id, model.Id);

        var document = new CurriculumVitaeDocument
        {
            Id = model.Id,
            RowVersion = model.Version,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            Location = model.Location,
            Bio = model.Bio,
            Skills = model.Skills
                .Select(s => new CvSkillDocument
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Level = s.Level
                })
                .ToList(),
            Educations = model.Educations
                .Select(e => new CvEducationDocument
                {
                    Id = e.Id,
                    Institution = e.Institution,
                    FieldOfStudy = e.FieldOfStudy,
                    Degree = e.Degree,
                    PeriodStart = e.PeriodStart,
                    PeriodEnd = e.PeriodEnd
                })
                .ToList(),
            Projects = model.Projects
                .Select(p => new CvProjectDocument
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Url = p.Url,
                    Technologies = p.Technologies.ToList(),
                    PeriodStart = p.PeriodStart,
                    PeriodEnd = p.PeriodEnd
                })
                .ToList(),
            WorkExperienceIds = model.WorkExperienceIds.ToList(),
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
        var filter = Builders<CurriculumVitaeDocument>.Filter.Eq(d => d.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    private static CurriculumVitaeDto MapToDto(CurriculumVitaeDocument d) => new(
        d.Id,
        d.RowVersion > 0 ? d.RowVersion : 1L,
        new PersonalInfoDto(d.FirstName, d.LastName, d.Email, d.Phone, d.Location, d.Bio),
        d.Skills
            .Select(s => new CvSkillDto(s.Id, s.Name, s.Category, s.Level))
            .ToList()
            .AsReadOnly(),
        d.Educations
            .Select(e => new CvEducationDto(
                e.Id,
                e.Institution,
                e.FieldOfStudy,
                e.Degree,
                e.PeriodStart,
                e.PeriodEnd))
            .ToList()
            .AsReadOnly(),
        d.Projects
            .Select(p => new CvProjectDto(
                p.Id,
                p.Name,
                p.Description,
                p.Url,
                p.Technologies.AsReadOnly(),
                p.PeriodStart,
                p.PeriodEnd))
            .ToList()
            .AsReadOnly(),
        d.WorkExperienceIds.AsReadOnly(),
        d.CreatedDate,
        d.ModifiedDate,
        d.LastModifiedAt);
}
