using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Authorization;

using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authorization;

namespace ServicePilot.Api.Authentication;

internal sealed class UserCapabilityAuthorizationHandler(
    IUserAuthorizationService authorizationService)
    : AuthorizationHandler<UserCapabilityRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserCapabilityRequirement requirement)
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

        bool hasCapability =
            await authorizationService.HasCapabilityAsync(
                organizationId,
                userId,
                requirement.Capability);

        if (hasCapability)
        {
            context.Succeed(requirement);
        }
    }
}