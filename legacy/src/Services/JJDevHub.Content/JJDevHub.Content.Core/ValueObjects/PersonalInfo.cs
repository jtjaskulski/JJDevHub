using JJDevHub.Shared.Kernel.BuildingBlocks;

namespace JJDevHub.Content.Core.ValueObjects;

public sealed class PersonalInfo : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string? Phone { get; }
    public string? Location { get; }
    public string? Bio { get; }

    public PersonalInfo(
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? location = null,
        string? bio = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        yield return Email;
        yield return Phone ?? "";
        yield return Location ?? "";
        yield return Bio ?? "";
    }
}
