using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Authentication.PasswordReset;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Authentication;

internal sealed class PasswordResetTokenRepository(ServicePilotDbContext dbContext)
    : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.PasswordResetTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> ListAvailableForUserAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.PasswordResetTokens.Where(token =>
            token.OrganizationId == organizationId
            && token.UserId == userId
            && token.UsedAtUtc == null
            && token.RevokedAtUtc == null).ToArrayAsync(cancellationToken);

    public void Add(PasswordResetToken token) => dbContext.PasswordResetTokens.Add(token);
}