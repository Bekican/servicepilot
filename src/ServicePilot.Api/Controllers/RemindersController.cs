using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;
using ServicePilot.Application.Reminders;
using ServicePilot.Contracts.Common;

using ContractResponse =
    ServicePilot.Contracts.Reminders.ReminderResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route("api/reminders")]
public sealed class RemindersController(
    ReminderManagementService service,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpGet]
    public async Task<ActionResult<
        PagedResponse<ContractResponse>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PageResult<ReminderResponse> result = await service.ListPageAsync(
            status, page, pageSize, cancellationToken);
        return Ok(new PagedResponse<ContractResponse>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy =
        AuthorizationPolicies.ReminderRetry)]
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status200OK)]
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
            : ToProblem(result.Error);
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