using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Users.Technicians;

using ContractResponse =
    ServicePilot.Contracts.Users.TechnicianResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy =
    AuthorizationPolicies.AppointmentManage)]
[Route("api/technicians")]
public sealed class TechniciansController(
    TechnicianDirectoryService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ContractResponse>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TechnicianResponse> technicians =
            await service.ListActiveAsync(cancellationToken);

        return Ok(technicians.Select(technician =>
            new ContractResponse(
                technician.Id,
                technician.FirstName,
                technician.LastName)));
    }
}