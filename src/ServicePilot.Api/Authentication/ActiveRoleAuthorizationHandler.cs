using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Authorization;

using ServicePilot.Application.Authentication;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.Api.Authentication;

internal sealed class ActiveRoleAuthorizationHandler(
    IUserRepository userRepository)
    : AuthorizationHandler<ActiveRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveRoleRequirement requirement)
    {
        string? userIdValue =
            context.User.FindFirst(
                JwtRegisteredClaimNames.Sub)?.Value;
        string? organizationIdValue =
            context.User.FindFirst(
                AuthenticationClaimNames.OrganizationId)?
                .Value;

        if (!Guid.TryParse(userIdValue, out Guid userId)
            || !Guid.TryParse(
                organizationIdValue,
                out Guid organizationId))
        {
            return;
        }

        User? user = await userRepository.GetByIdAsync(
            organizationId,
            userId);

        if (user is null || !user.IsActive)
        {
            return;
        }

        if (requirement.AllowedRoles.Count == 0
            || requirement.AllowedRoles.Contains(user.Role))
        {
            context.Succeed(requirement);
        }
    }
}