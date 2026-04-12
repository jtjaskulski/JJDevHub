using JJDevHub.Content.Core.Exceptions;

namespace JJDevHub.Content.Core.ValueObjects;

public sealed class CompanyInfo : IEquatable<CompanyInfo>
{
    public string CompanyName { get; }
    public string? Location { get; }
    public string? WebsiteUrl { get; }
    public string? Industry { get; }

    public CompanyInfo(string companyName, string? location, string? websiteUrl, string? industry)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.COMPANY_EMPTY", "Company name is required.");
        if (companyName.Length > 200)
            throw new ContentDomainException("CONTENT.JOB_APPLICATION.COMPANY_MAX", "Company name is too long.");

        CompanyName = companyName.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        Industry = string.IsNullOrWhiteSpace(industry) ? null : industry.Trim();
    }

    private CompanyInfo()
    {
        CompanyName = null!;
    }

    public bool Equals(CompanyInfo? other)
    {
        if (other is null) return false;
        return CompanyName == other.CompanyName
               && Location == other.Location
               && WebsiteUrl == other.WebsiteUrl
               && Industry == other.Industry;
    }

    public override bool Equals(object? obj) => obj is CompanyInfo other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(CompanyName, Location, WebsiteUrl, Industry);
}
