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

    public void Add(User user)
    {
        _users.Add(user);
    }

    public void Seed(User user)
    {
        _users.Add(user);
    }
}