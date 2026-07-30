using System.IdentityModel.Tokens.Jwt;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;

namespace ServicePilot.Api.Authentication;

internal sealed class HttpTenantContext(
    IHttpContextAccessor httpContextAccessor)
    : ITenantContext, ICurrentUserContext
{
    public Guid UserId =>
        GetRequiredGuidClaim(
            JwtRegisteredClaimNames.Sub,
            "user");

    public Guid OrganizationId
    {
        get => GetRequiredGuidClaim(
            AuthenticationClaimNames.OrganizationId,
            "organization");
    }

    public string Role =>
        httpContextAccessor.HttpContext?
            .User
            .FindFirst(AuthenticationClaimNames.Role)?
            .Value
        ?? throw new InvalidOperationException(
            "Authenticated request does not contain a role");

    private Guid GetRequiredGuidClaim(
        string claimName,
        string claimDescription)
    {
        string? claimValue =
            httpContextAccessor.HttpContext?
                .User
                .FindFirst(claimName)?
                .Value;

        if (!Guid.TryParse(
            claimValue,
            out Guid identifier))
        {
            throw new InvalidOperationException(
                $"Authenticated request does not contain a valid {claimDescription} identifier");
        }

        return identifier;
    }
}