namespace ServicePilot.Contracts.Organizations.CreateOrganization;

public sealed record CreateOrganizationRequest(
    string Name,
    string Slug
);