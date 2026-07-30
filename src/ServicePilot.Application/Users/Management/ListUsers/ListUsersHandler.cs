using ServicePilot.Application.Abstractions.Tenancy;

namespace ServicePilot.Application.Users.Management.ListUsers;

public sealed class ListUsersHandler(
    ITenantContext tenantContext,
    IUserRepository userRepository)
{
    public async Task<IReadOnlyList<UserResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Users.User> users =
            await userRepository.ListAsync(
                tenantContext.OrganizationId,
                cancellationToken);

        return users
            .Select(UserResponse.FromUser)
            .ToArray();
    }
}