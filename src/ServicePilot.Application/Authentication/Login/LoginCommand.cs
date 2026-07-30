namespace ServicePilot.Application.Authentication.Login;

public sealed record LoginCommand(
    string OrganizationSlug,
    string Email,
    string Password);