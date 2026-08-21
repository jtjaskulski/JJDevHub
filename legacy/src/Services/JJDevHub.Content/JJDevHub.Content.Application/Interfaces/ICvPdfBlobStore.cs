namespace JJDevHub.Content.Application.Interfaces;

public interface ICvPdfBlobStore
{
    Task<Guid> SaveAsync(
        Guid curriculumVitaeId,
        Guid? jobApplicationId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken);

    Task<(byte[] Content, string FileName)?> GetAsync(Guid id, CancellationToken cancellationToken);
}
