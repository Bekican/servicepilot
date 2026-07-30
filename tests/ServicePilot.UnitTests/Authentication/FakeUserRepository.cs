using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Authentication;

internal sealed class FakeUserRepository
    : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users =>
        _users.AsReadOnly();

    public Task<User?> GetByOrganizationAndEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        User? user = _users.SingleOrDefault(
            candidate =>
                candidate.OrganizationId == organizationId
                && candidate.Email == email);

        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = _users.SingleOrDefault(
            candidate =>
                candidate.OrganizationId == organizationId
                && candidate.Id == userId);

        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> users = _users
            .Where(user =>
                user.OrganizationId == organizationId)
            .ToArray();

        return Task.FromResult(users);
    }

    public Task<int> CountActiveOwnersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        int count = _users.Count(
            user =>
                user.OrganizationId == organizationId
                && user.Role == UserRoles.Owner
                && user.IsActive);

        return Task.FromResult(count);
    }

    public void Add(User user)
    {
        _users.Add(user);
    }

    public void Seed(User user)
    {
        _users.Add(user);
    }
}