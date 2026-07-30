using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Dashboard;
using ServicePilot.Contracts.Dashboard;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy =
    AuthorizationPolicies.DashboardView)]
[Route("api/dashboard")]
public sealed class DashboardController(
    DashboardService service)
    : ControllerBase
{
    [HttpGet("summary")]
    public async Task<
        ActionResult<DashboardSummaryResponse>> GetSummary(
        CancellationToken cancellationToken)
    {
        DashboardSummary summary =
            await service.GetSummaryAsync(
                cancellationToken);

        return Ok(new DashboardSummaryResponse(
            summary.DateUtc,
            summary.ActiveCustomerCount,
            summary.ActiveUserCount,
            summary.ScheduledAppointmentCount,
            summary.ConfirmedAppointmentCount,
            summary.InProgressAppointmentCount,
            summary.CompletedAppointmentCount,
            summary.CancelledAppointmentCount,
            summary.FailedReminderCount));
    }
}