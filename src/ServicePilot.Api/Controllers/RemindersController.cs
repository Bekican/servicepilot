using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Application.Reminders;

using ContractResponse =
    ServicePilot.Contracts.Reminders.ReminderResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route("api/reminders")]
public sealed class RemindersController(
    ReminderManagementService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ContractResponse>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReminderResponse> reminders =
            await service.ListAsync(
                status,
                cancellationToken);

        return Ok(reminders.Select(Map));
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy =
        AuthorizationPolicies.ReminderRetry)]
    public async Task<IActionResult> Retry(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ReminderResponse> result =
            await service.RetryAsync(
                id,
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Problem(
                statusCode:
                    result.Error == ReminderErrors.NotFound
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status409Conflict,
                title: result.Error.Code,
                detail: result.Error.Message);
    }

    private static ContractResponse Map(
        ReminderResponse response)
    {
        return new ContractResponse(
            response.Id,
            response.AppointmentId,
            response.Status,
            response.AttemptCount,
            response.ScheduledAtUtc,
            response.NextAttemptAtUtc,
            response.LastAttemptAtUtc,
            response.LastError,
            response.UpdatedAtUtc);
    }
}