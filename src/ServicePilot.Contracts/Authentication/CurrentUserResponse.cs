namespace ServicePilot.Contracts.Authentication;

public sealed record CurrentUserResponse(
    Guid UserId,
    Guid OrganizationId,
    string Role);