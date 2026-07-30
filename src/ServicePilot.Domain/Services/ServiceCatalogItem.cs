namespace ServicePilot.Domain.Services;

public sealed class ServiceCatalogItem
{
    public const int MaxNameLength = 200;
    public const int MinDurationMinutes = 1;
    public const int MaxDurationMinutes = 1440;

    private ServiceCatalogItem()
    {
    }

    public ServiceCatalogItem(
        Guid id,
        Guid organizationId,
        string name,
        int defaultDurationMinutes,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty
            || organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifiers cannot be empty");
        }

        Id = id;
        OrganizationId = organizationId;
        SetDetails(name, defaultDurationMinutes);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } =
        string.Empty;
    public int DefaultDurationMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string name,
        int defaultDurationMinutes,
        DateTimeOffset updatedAtUtc)
    {
        SetDetails(name, defaultDurationMinutes);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetActive(
        bool isActive,
        DateTimeOffset updatedAtUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void SetDetails(
        string name,
        int defaultDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException(
                "Service name is invalid",
                nameof(name));
        }

        if (defaultDurationMinutes
            is < MinDurationMinutes
            or > MaxDurationMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultDurationMinutes));
        }

        Name = name.Trim();
        NormalizedName = Name.ToLowerInvariant();
        DefaultDurationMinutes = defaultDurationMinutes;
    }
}