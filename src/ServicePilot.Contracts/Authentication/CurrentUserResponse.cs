namespace ServicePilot.Contracts.Authentication;

public sealed record CurrentUserResponse(
    Guid UserId,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string TimeZoneId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    IReadOnlyList<string> Capabilities);