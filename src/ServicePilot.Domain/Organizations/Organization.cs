namespace ServicePilot.Domain.Organizations;

public sealed class Organization
{
    private Organization()
    {
    }

    public Organization(
        Guid id,
        string name,
        string slug,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Organization name cannot be empty",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException(
                "Organization slug cannot be empty.",
                nameof(slug));
        }

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}