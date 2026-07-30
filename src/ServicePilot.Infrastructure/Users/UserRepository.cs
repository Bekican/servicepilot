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
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.OrganizationId == organizationId
                    && user.Email == email,
                cancellationToken);
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}