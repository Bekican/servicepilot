namespace ServicePilot.Application.Services;

public sealed record ServiceCatalogData(
    string Name,
    int DefaultDurationMinutes);

public sealed record ServiceCatalogResponse(
    Guid Id,
    string Name,
    int DefaultDurationMinutes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);