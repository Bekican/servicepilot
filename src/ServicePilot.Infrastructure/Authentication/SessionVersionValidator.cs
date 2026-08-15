using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Authentication;

public sealed class SessionVersionValidator(IUserRepository userRepository)
    : ISessionVersionValidator
{
    public async Task<bool> IsValidAsync(
        Guid organizationId,
        Guid userId,
        int sessionVersion,
        CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetByIdAsync(
            organizationId,
            userId,
            cancellationToken);

        return user is { IsActive: true }
            && user.SessionVersion == sessionVersion;
    }
}
