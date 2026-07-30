namespace ServicePilot.Contracts.Authentication;

public sealed record LoginRequest(
    string OrganizationSlug,
    string Email,
    string Password);