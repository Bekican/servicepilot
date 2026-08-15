using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Threading.RateLimiting;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.AcceptInvitation;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Application.Authentication.PasswordReset;
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
    PasswordResetService passwordResetService,
    PasswordResetRequestLimiter passwordResetRequestLimiter,
    CurrentSessionService currentSessionService,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
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
            request.Password,
            request.TimeZoneId);

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

    [AllowAnonymous]
    [DisableRateLimiting]
    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(
        PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        using RateLimitLease lease = await passwordResetRequestLimiter.AcquireAsync(
            request.OrganizationSlug,
            request.Email,
            cancellationToken);
        if (!lease.IsAcquired)
        {
            if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            {
                Response.Headers.RetryAfter = Math.Max(
                    1,
                    (int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(CultureInfo.InvariantCulture);
            }
            return ToProblem(
                ApiProblemCodes.RateLimitExceeded,
                StatusCodes.Status429TooManyRequests,
                "Too many password reset requests. Try again later.");
        }

        await passwordResetService.RequestAsync(
            request.OrganizationSlug,
            request.Email,
            cancellationToken);
        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost("password-reset/complete")]
    public async Task<IActionResult> CompletePasswordReset(
        CompletePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await passwordResetService.CompleteAsync(
            request.Token,
            request.Password,
            cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(
        CancellationToken cancellationToken)
    {
        CurrentSessionResponse? session =
            await currentSessionService.GetAsync(
                cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            session.UserId,
            session.OrganizationId,
            session.OrganizationName,
            session.OrganizationSlug,
            session.TimeZoneId,
            session.FirstName,
            session.LastName,
            session.Email,
            session.Role,
            session.Capabilities));
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
