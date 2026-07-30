namespace ServicePilot.Contracts.Services;

public sealed record ServiceUpsertRequest(
    string Name,
    int DefaultDurationMinutes);

public sealed record ServiceStatusRequest(
    bool IsActive);

public sealed record ServiceResponse(
    Guid Id,
    string Name,
    int DefaultDurationMinutes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);