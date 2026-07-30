namespace ServicePilot.Application.Users.Invitations.CreateInvitation;

public sealed record CreateInvitationCommand(
    string Email,
    string Role);