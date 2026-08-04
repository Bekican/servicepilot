using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
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
    AppointmentService service,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpPost]
    [Authorize(Policy =
        AuthorizationPolicies.AppointmentManage)]
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status201Created)]
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
    [ProducesResponseType(
        typeof(IReadOnlyList<ContractResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] Guid? technicianId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ApplicationResponse>> result =
            await service.ListAsync(
                new AppointmentFilterData(
                    from,
                    to,
                    status,
                    technicianId),
                cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value.Select(Map))
            : ToProblem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status200OK)]
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
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status200OK)]
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
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status200OK)]
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

    private static ContractResponse Map(
        ApplicationResponse response)
    {
        return new ContractResponse(
            response.Id,
            response.CustomerId,
            response.CustomerNumber,
            response.CustomerDisplayName,
            response.ServiceId,
            response.ServiceName,
            response.TechnicianUserId,
            response.TechnicianDisplayName,
            response.StartAtUtc,
            response.EndAtUtc,
            response.Status,
            response.AllowedTransitions,
            response.CanAssignTechnician,
            response.CreatedAtUtc,
            response.UpdatedAtUtc);
    }
}
