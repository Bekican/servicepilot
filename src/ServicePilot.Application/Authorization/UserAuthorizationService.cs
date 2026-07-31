using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authorization;

public sealed class UserAuthorizationService(
    IUserRepository userRepository)
    : IUserAuthorizationService
{
    public async Task<IReadOnlyList<UserCapability>>
        GetCapabilitiesAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty
            || userId == Guid.Empty)
        {
            return [];
        }

        User? user = await userRepository.GetByIdAsync(
            organizationId,
            userId,
            cancellationToken);

        return user is { IsActive: true }
            ? GetCapabilities(user.Role)
            : [];
    }

    public async Task<bool> HasCapabilityAsync(
        Guid organizationId,
        Guid userId,
        UserCapability capability,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty
            || userId == Guid.Empty)
        {
            return false;
        }

        IReadOnlyList<UserCapability> capabilities =
            await GetCapabilitiesAsync(
                organizationId,
                userId,
                cancellationToken);

        return capabilities.Contains(capability);
    }

    private static IReadOnlyList<UserCapability>
        GetCapabilities(string role)
    {
        return role switch
        {
            UserRoles.Owner =>
                Enum.GetValues<UserCapability>(),
            UserRoles.Admin =>
            [
                UserCapability.AccessSystem,
                UserCapability.ManageCustomers,
                UserCapability.ManageServices,
                UserCapability.ManageAppointments,
                UserCapability.ViewDashboard,
                UserCapability.RetryReminders
            ],
            UserRoles.Dispatcher =>
            [
                UserCapability.AccessSystem,
                UserCapability.ManageCustomers,
                UserCapability.ManageAppointments,
                UserCapability.RetryReminders
            ],
            UserRoles.Technician =>
            [
                UserCapability.AccessSystem
            ],
            _ => []
        };
    }
}