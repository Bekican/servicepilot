namespace ServicePilot.Contracts.Users.Invitations;

public sealed record UserInvitationResponse(
    Guid Id,
    string Email,
    string Role,
    DateTimeOffset ExpiresAtUtc);