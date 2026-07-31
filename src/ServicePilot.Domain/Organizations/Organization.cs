namespace ServicePilot.Domain.Organizations;

public sealed class Organization
{
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 100;
    public const int MaxTimeZoneIdLength = 100;

    private Organization()
    {
    }

    public Organization(
        Guid id,
        string name,
        string slug,
        DateTimeOffset createdAtUtc)
        : this(
            id,
            name,
            slug,
            "UTC",
            createdAtUtc)
    {
    }

    public Organization(
        Guid id,
        string name,
        string slug,
        string timeZoneId,
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

        if (string.IsNullOrWhiteSpace(timeZoneId)
            || timeZoneId.Trim().Length
                > MaxTimeZoneIdLength)
        {
            throw new ArgumentException(
                "Organization time zone is invalid",
                nameof(timeZoneId));
        }

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        TimeZoneId = timeZoneId.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "UTC";

    public DateTimeOffset CreatedAtUtc { get; private set; }
}