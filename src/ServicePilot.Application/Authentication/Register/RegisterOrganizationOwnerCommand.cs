namespace ServicePilot.Application.Authentication.Register;

public sealed record RegisterOrganizationOwnerCommand(
    string OrganizationName,
    string OrganizationSlug,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string TimeZoneId = "UTC");