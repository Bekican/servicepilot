namespace ServicePilot.Application.Authentication;

public sealed record AuthenticationResponse(
    Guid UserId,
    Guid OrganizationId,
    string Role,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);