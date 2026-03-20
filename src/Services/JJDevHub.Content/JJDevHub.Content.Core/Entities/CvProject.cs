using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CvProject : Entity
{
    public Guid CurriculumVitaeId { get; internal set; }

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? Url { get; private set; }

    private readonly List<string> _technologies = new();
    public IReadOnlyList<string> Technologies => _technologies;

    public DateRange Period { get; private set; } = null!;

    private CvProject() { }

    internal CvProject(
        string name,
        string description,
        string? url,
        IReadOnlyList<string> technologies,
        DateRange period)
    {
        Name = name.Trim();
        Description = description.Trim();
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        _technologies.AddRange(technologies.Select(t => t.Trim()).Where(t => t.Length > 0));
        Period = period;
    }
}
