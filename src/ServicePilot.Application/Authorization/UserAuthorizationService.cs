using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authorization;

public sealed class UserAuthorizationService(
    IUserRepository userRepository)
    : IUserAuthorizationService
{
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

        User? user = await userRepository.GetByIdAsync(
            organizationId,
            userId,
            cancellationToken);

        return user is { IsActive: true }
            && HasCapability(user.Role, capability);
    }

    private static bool HasCapability(
        string role,
        UserCapability capability)
    {
        return role switch
        {
            UserRoles.Owner => true,
            UserRoles.Admin => capability is
                UserCapability.AccessSystem
                or UserCapability.ManageCustomers
                or UserCapability.ManageServices
                or UserCapability.ManageAppointments
                or UserCapability.ViewDashboard
                or UserCapability.RetryReminders,
            UserRoles.Dispatcher => capability is
                UserCapability.AccessSystem
                or UserCapability.ManageCustomers
                or UserCapability.ManageAppointments
                or UserCapability.RetryReminders,
            UserRoles.Technician =>
                capability == UserCapability.AccessSystem,
            _ => false
        };
    }
}