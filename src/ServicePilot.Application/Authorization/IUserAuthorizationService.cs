namespace ServicePilot.Application.Authorization;

public interface IUserAuthorizationService
{
    Task<IReadOnlyList<UserCapability>>
        GetCapabilitiesAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default);

    Task<bool> HasCapabilityAsync(
        Guid organizationId,
        Guid userId,
        UserCapability capability,
        CancellationToken cancellationToken = default);
}