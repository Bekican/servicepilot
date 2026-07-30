using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users;

public interface IUserInvitationRepository
{
    Task<UserInvitation?> GetByIdAsync(
        Guid organizationId,
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task<UserInvitation?> GetPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    Task<UserInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void Add(UserInvitation invitation);
}