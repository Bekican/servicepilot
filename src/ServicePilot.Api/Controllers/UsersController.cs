using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;
using ServicePilot.Application.Users.Management;
using ServicePilot.Application.Users.Management.ChangeUserRole;
using ServicePilot.Application.Users.Management.ChangeUserStatus;
using ServicePilot.Application.Users.Management.ListUsers;
using ServicePilot.Contracts.Users;

using ApplicationUserResponse =
    ServicePilot.Application.Users.Management.UserResponse;
using ContractUserResponse =
    ServicePilot.Contracts.Users.UserResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ManageUsers)]
[Route("api/users")]
public sealed class UsersController(
    ListUsersHandler listHandler,
    ChangeUserRoleHandler changeRoleHandler,
    ChangeUserStatusHandler changeStatusHandler,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<ContractUserResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyList<ContractUserResponse>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationUserResponse> users =
            await listHandler.HandleAsync(
                cancellationToken);

        return Ok(users.Select(MapResponse));
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(
        typeof(ContractUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationUserResponse> result =
            await changeRoleHandler.HandleAsync(
                new ChangeUserRoleCommand(
                    id,
                    request.Role),
                cancellationToken);

        return result.IsSuccess
            ? Ok(MapResponse(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(
        typeof(ContractUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationUserResponse> result =
            await changeStatusHandler.HandleAsync(
                new ChangeUserStatusCommand(
                    id,
                    request.IsActive),
                cancellationToken);

        return result.IsSuccess
            ? Ok(MapResponse(result.Value))
            : ToProblem(result.Error);
    }

    private static ContractUserResponse MapResponse(
        ApplicationUserResponse response)
    {
        return new ContractUserResponse(
            response.Id,
            response.FirstName,
            response.LastName,
            response.Email,
            response.Role,
            response.IsActive,
            response.CreatedAtUtc);
    }
}
