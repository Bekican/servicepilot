namespace ServicePilot.Application.Users.Invitations.ResendInvitation;

public sealed record ResendInvitationCommand(
    Guid InvitationId);