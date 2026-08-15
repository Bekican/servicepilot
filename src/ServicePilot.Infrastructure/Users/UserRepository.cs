using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Users;

internal sealed class UserRepository(
    ServicePilotDbContext dbContext)
    : IUserRepository
{
    public Task<User?> GetByOrganizationAndEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(
                user =>
                    user.OrganizationId == organizationId
                    && user.Email == email,
                cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(
                user =>
                    user.OrganizationId == organizationId
                    && user.Id == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.OrganizationId == organizationId)
            .OrderBy(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountActiveOwnersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(
            user =>
                user.OrganizationId == organizationId
                && user.Role == UserRoles.Owner
                && user.IsActive,
            cancellationToken);
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}