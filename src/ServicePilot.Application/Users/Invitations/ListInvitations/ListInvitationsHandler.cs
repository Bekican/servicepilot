using ServicePilot.Application.Abstractions.Authentication;

using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Invitations.ListInvitations;

public sealed class ListInvitationsHandler(
    ICurrentUserContext currentUser,
    IUserInvitationRepository invitationRepository)
{
    public async Task<IReadOnlyList<InvitationResponse>>
        HandleAsync(
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserInvitation> invitations =
            await invitationRepository.ListPendingAsync(
                currentUser.OrganizationId,
                cancellationToken);

        return invitations.Select(invitation =>
            new InvitationResponse(
                invitation.Id,
                invitation.Email,
                invitation.Role,
                invitation.ExpiresAtUtc)).ToArray();
    }
}