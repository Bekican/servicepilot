using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Appointments;
using ServicePilot.Application.Common;
using ServicePilot.Contracts.Appointments;

using ApplicationResponse =
    ServicePilot.Application.Appointments.AppointmentResponse;
using ContractResponse =
    ServicePilot.Contracts.Appointments.AppointmentResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route("api/appointments")]
public sealed class AppointmentsController(
    AppointmentService service)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy =
        AuthorizationPolicies.AppointmentManage)]
    public async Task<IActionResult> Create(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationResponse> result =
            await service.CreateAsync(
                new CreateAppointmentData(
                    request.CustomerId,
                    request.ServiceId,
                    request.TechnicianUserId,
                    request.StartAt,
                    request.EndAt),
                cancellationToken);

        return result.IsSuccess
            ? Created(
                $"/api/appointments/{result.Value.Id}",
                Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ContractResponse>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationResponse> appointments =
            await service.ListAsync(cancellationToken);

        return Ok(appointments.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ApplicationResponse> result =
            await service.GetAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/technician")]
    [Authorize(Policy =
        AuthorizationPolicies.AppointmentManage)]
    public async Task<IActionResult> Assign(
        Guid id,
        AssignTechnicianRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationResponse> result =
            await service.AssignAsync(
                id,
                request.TechnicianUserId,
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Transition(
        Guid id,
        AppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationResponse> result =
            await service.TransitionAsync(
                id,
                request.Status,
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode =
            error == AppointmentErrors.NotFound
                ? StatusCodes.Status404NotFound
                : error == AppointmentErrors.Forbidden
                    ? StatusCodes.Status403Forbidden
                    : error
                        == AppointmentErrors
                            .TechnicianOverlap
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest;

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }

    private static ContractResponse Map(
        ApplicationResponse response)
    {
        return new ContractResponse(
            response.Id,
            response.CustomerId,
            response.ServiceId,
            response.TechnicianUserId,
            response.StartAtUtc,
            response.EndAtUtc,
            response.Status,
            response.CreatedAtUtc,
            response.UpdatedAtUtc);
    }
}