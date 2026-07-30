namespace ServicePilot.Contracts.Organizations;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    DateTimeOffset CreatedAtUtc
);