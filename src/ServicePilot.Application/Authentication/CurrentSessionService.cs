using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Authorization;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Users;

using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication;

public sealed class CurrentSessionService(
    ICurrentUserContext currentUser,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IUserAuthorizationService authorizationService)
{
    public async Task<CurrentSessionResponse?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetByIdAsync(
            currentUser.OrganizationId,
            currentUser.UserId,
            cancellationToken);
        Organization? organization =
            await organizationRepository.GetByIdAsync(
                currentUser.OrganizationId,
                cancellationToken);

        if (user is not { IsActive: true }
            || organization is null)
        {
            return null;
        }

        IReadOnlyList<UserCapability> capabilities =
            await authorizationService.GetCapabilitiesAsync(
                currentUser.OrganizationId,
                currentUser.UserId,
                cancellationToken);

        return new CurrentSessionResponse(
            user.Id,
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.TimeZoneId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            capabilities.Select(capability =>
                capability.ToString()).ToArray());
    }
}

public sealed record CurrentSessionResponse(
    Guid UserId,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string TimeZoneId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    IReadOnlyList<string> Capabilities);