using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Users;

internal sealed class UserInvitationRepository(
    ServicePilotDbContext dbContext)
    : IUserInvitationRepository
{
    public async Task<IReadOnlyList<UserInvitation>>
        ListPendingAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.UserInvitations
            .AsNoTracking()
            .Where(invitation =>
                invitation.OrganizationId == organizationId
                && invitation.Status
                    == UserInvitationStatus.Pending)
            .OrderBy(invitation =>
                invitation.ExpiresAtUtc)
            .ThenBy(invitation => invitation.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<UserInvitation?> GetByIdAsync(
        Guid organizationId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserInvitations
            .SingleOrDefaultAsync(
                invitation =>
                    invitation.OrganizationId
                        == organizationId
                    && invitation.Id == invitationId,
                cancellationToken);
    }

    public Task<UserInvitation?> GetPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserInvitations
            .SingleOrDefaultAsync(
                invitation =>
                    invitation.OrganizationId
                        == organizationId
                    && invitation.Email == email
                    && invitation.Status
                        == UserInvitationStatus.Pending,
                cancellationToken);
    }

    public Task<UserInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserInvitations
            .SingleOrDefaultAsync(
                invitation =>
                    invitation.TokenHash == tokenHash,
                cancellationToken);
    }

    public void Add(UserInvitation invitation)
    {
        dbContext.UserInvitations.Add(invitation);
    }
}