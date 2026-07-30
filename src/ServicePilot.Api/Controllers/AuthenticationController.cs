using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.AcceptInvitation;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Application.Authentication.Register;
using ServicePilot.Application.Common;
using ServicePilot.Application.Users.Invitations;
using ServicePilot.Contracts.Authentication;

namespace ServicePilot.Api.Controllers;

[ApiController]
[EnableRateLimiting("authentication")]
[Route("api/auth")]
public sealed class AuthenticationController(
    RegisterOrganizationOwnerHandler registerHandler,
    LoginHandler loginHandler,
    AcceptInvitationHandler acceptInvitationHandler,
    ITenantContext tenantContext)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthenticationTokenResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        RegisterOrganizationOwnerCommand command = new(
            request.OrganizationName,
            request.OrganizationSlug,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        Result<AuthenticationResponse> result =
            await registerHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        AuthenticationTokenResponse response =
            MapResponse(result.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthenticationTokenResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        LoginCommand command = new(
            request.OrganizationSlug,
            request.Email,
            request.Password);

        Result<AuthenticationResponse> result =
            await loginHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Ok(MapResponse(result.Value));
    }

    [AllowAnonymous]
    [HttpPost("invitations/accept")]
    [ProducesResponseType(
        typeof(AuthenticationTokenResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        AcceptInvitationCommand command = new(
            request.Token,
            request.FirstName,
            request.LastName,
            request.Password);

        Result<AuthenticationResponse> result =
            await acceptInvitationHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Ok(MapResponse(result.Value));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        string? role =
            User.FindFirst(
                AuthenticationClaimNames.Role)?.Value;

        if (!Guid.TryParse(
            userIdValue,
            out Guid userId)
            || string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            userId,
            tenantContext.OrganizationId,
            role));
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode = error switch
        {
            _ when error
                == AuthenticationErrors.InvalidCredentials =>
                    StatusCodes.Status401Unauthorized,
            _ when error
                == AuthenticationErrors.OrganizationSlugAlreadyExists =>
                    StatusCodes.Status409Conflict,
            _ when error
                == AuthenticationErrors.EmailAlreadyExists =>
                    StatusCodes.Status409Conflict,
            _ when error
                == InvitationErrors.UserAlreadyExists =>
                    StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }

    private static AuthenticationTokenResponse MapResponse(
        AuthenticationResponse response)
    {
        return new AuthenticationTokenResponse(
            response.UserId,
            response.OrganizationId,
            response.Role,
            response.AccessToken,
            response.ExpiresAtUtc);
    }
}