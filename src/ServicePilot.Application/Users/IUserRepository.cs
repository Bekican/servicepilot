using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByOrganizationAndEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    void Add(User user);
}