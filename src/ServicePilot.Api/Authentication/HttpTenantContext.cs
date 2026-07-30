using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;

namespace ServicePilot.Api.Authentication;

internal sealed class HttpTenantContext(
    IHttpContextAccessor httpContextAccessor)
    : ITenantContext
{
    public Guid OrganizationId
    {
        get
        {
            string? organizationIdValue =
                httpContextAccessor.HttpContext?
                    .User
                    .FindFirst(
                        AuthenticationClaimNames.OrganizationId)?
                    .Value;

            if (!Guid.TryParse(
                organizationIdValue,
                out Guid organizationId))
            {
                throw new InvalidOperationException(
                    "Authenticated request does not contain a valid organization identifier");
            }

            return organizationId;
        }
    }
}