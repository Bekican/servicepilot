namespace ServicePilot.Application.Users.Invitations;

public sealed record InvitationResponse(
    Guid Id,
    string Email,
    string Role,
    DateTimeOffset ExpiresAtUtc);