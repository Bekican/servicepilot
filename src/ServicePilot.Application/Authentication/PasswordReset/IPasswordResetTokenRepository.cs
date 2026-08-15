using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication.PasswordReset;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetToken>> ListAvailableForUserAsync(
        Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    void Add(PasswordResetToken token);
}