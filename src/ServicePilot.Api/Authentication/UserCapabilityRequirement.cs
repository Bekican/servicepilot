using Microsoft.AspNetCore.Authorization;

using ServicePilot.Application.Authorization;

namespace ServicePilot.Api.Authentication;

internal sealed record UserCapabilityRequirement(
    UserCapability Capability)
    : IAuthorizationRequirement;