using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByOrganizationAndEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveOwnersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    void Add(User user);
}