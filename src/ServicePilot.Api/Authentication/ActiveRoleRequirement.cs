using Microsoft.AspNetCore.Authorization;

namespace ServicePilot.Api.Authentication;

internal sealed class ActiveRoleRequirement(
    params string[] allowedRoles)
    : IAuthorizationRequirement
{
    public IReadOnlySet<string> AllowedRoles { get; } =
        allowedRoles.ToHashSet(StringComparer.Ordinal);
}