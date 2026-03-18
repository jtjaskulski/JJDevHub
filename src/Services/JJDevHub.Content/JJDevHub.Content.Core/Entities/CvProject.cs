using JJDevHub.Content.Core.ValueObjects;
using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.Entities;

public class CvProject : Entity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? Url { get; private set; }
    public IReadOnlyList<string> Technologies { get; private set; } = Array.Empty<string>();
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
        Technologies = technologies.Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        Period = period;
    }
}
