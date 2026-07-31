namespace ServicePilot.Application.Authorization;

public interface IUserAuthorizationService
{
    Task<bool> HasCapabilityAsync(
        Guid organizationId,
        Guid userId,
        UserCapability capability,
        CancellationToken cancellationToken = default);
}