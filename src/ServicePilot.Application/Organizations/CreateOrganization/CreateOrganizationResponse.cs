namespace ServicePilot.Application.Organizations.CreateOrganization;

public sealed record CreateOrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    DateTimeOffset CreatedAtUtc);

    