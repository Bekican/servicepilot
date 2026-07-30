using Microsoft.AspNetCore.Authorization;

namespace ServicePilot.Api.Authentication;

internal sealed class ActiveOwnerRequirement
    : IAuthorizationRequirement;