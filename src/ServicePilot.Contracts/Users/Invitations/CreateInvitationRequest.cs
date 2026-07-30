namespace ServicePilot.Contracts.Users.Invitations;

public sealed record CreateInvitationRequest(
    string Email,
    string Role);