namespace ServicePilot.Contracts.Authentication;

public sealed record RegisterRequest(
    string OrganizationName,
    string OrganizationSlug,
    string FirstName,
    string LastName,
    string Email,
    string Password);