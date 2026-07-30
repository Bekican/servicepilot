namespace ServicePilot.Contracts.Authentication;

public sealed record AuthenticationTokenResponse(
    Guid UserId,
    Guid OrganizationId,
    string Role,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);