namespace ServicePilot.Contracts.Authentication;

public sealed record PasswordResetRequest(
    string OrganizationSlug,
    string Email);

public sealed record CompletePasswordResetRequest(
    string Token,
    string Password);