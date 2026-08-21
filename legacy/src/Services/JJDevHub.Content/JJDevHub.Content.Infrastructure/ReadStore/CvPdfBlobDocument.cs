namespace JJDevHub.Content.Infrastructure.ReadStore;

public sealed class CvPdfBlobDocument
{
    public Guid Id { get; set; }
    public Guid CurriculumVitaeId { get; set; }
    public Guid? JobApplicationId { get; set; }
    public string FileName { get; set; } = null!;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
}
