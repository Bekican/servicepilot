using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;
using ServicePilot.Application.Users.Invitations;
using ServicePilot.Application.Users.Invitations.CreateInvitation;
using ServicePilot.Application.Users.Invitations.ListInvitations;
using ServicePilot.Application.Users.Invitations.ResendInvitation;
using ServicePilot.Contracts.Users.Invitations;

namespace ServicePilot.Api.Controllers;

[ApiController]
[EnableRateLimiting("invitations")]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
[Route("api/users/invitations")]
public sealed class UserInvitationsController(
    CreateInvitationHandler createHandler,
    ResendInvitationHandler resendHandler,
    ListInvitationsHandler listHandler,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserInvitationResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyList<UserInvitationResponse>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<InvitationResponse> invitations =
            await listHandler.HandleAsync(
                cancellationToken);

        return Ok(invitations.Select(MapResponse));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(UserInvitationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        Result<InvitationResponse> result =
            await createHandler.HandleAsync(
                new CreateInvitationCommand(
                    request.Email,
                    request.Role),
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        UserInvitationResponse response =
            MapResponse(result.Value);

        return Created(
            $"/api/users/invitations/{response.Id}",
            response);
    }

    [HttpPost("{id:guid}/resend")]
    [ProducesResponseType(
        typeof(UserInvitationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Resend(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<InvitationResponse> result =
            await resendHandler.HandleAsync(
                new ResendInvitationCommand(id),
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Ok(MapResponse(result.Value));
    }

    private static UserInvitationResponse MapResponse(
        InvitationResponse response)
    {
        return new UserInvitationResponse(
            response.Id,
            response.Email,
            response.Role,
            response.ExpiresAtUtc);
    }
}
