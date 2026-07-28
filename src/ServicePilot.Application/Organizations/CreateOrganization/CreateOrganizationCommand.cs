namespace ServicePilot.Application.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string Slug);
