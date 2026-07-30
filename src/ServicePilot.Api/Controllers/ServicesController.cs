using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Application.Services;
using ServicePilot.Contracts.Services;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route("api/services")]
public sealed class ServicesController(
    ServiceCatalogService service)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy =
        AuthorizationPolicies.ServiceWrite)]
    public async Task<IActionResult> Create(
        ServiceUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ServiceCatalogResponse> result =
            await service.CreateAsync(
                new ServiceCatalogData(
                    request.Name,
                    request.DefaultDurationMinutes),
                cancellationToken);

        return result.IsSuccess
            ? Created(
                $"/api/services/{result.Value.Id}",
                Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ServiceResponse>>> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceCatalogResponse> services =
            await service.ListAsync(
                includeInactive,
                cancellationToken);

        return Ok(services.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ServiceCatalogResponse> result =
            await service.GetAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy =
        AuthorizationPolicies.ServiceWrite)]
    public async Task<IActionResult> Update(
        Guid id,
        ServiceUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ServiceCatalogResponse> result =
            await service.UpdateAsync(
                id,
                new ServiceCatalogData(
                    request.Name,
                    request.DefaultDurationMinutes),
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy =
        AuthorizationPolicies.ServiceWrite)]
    public async Task<IActionResult> SetStatus(
        Guid id,
        ServiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        Result<ServiceCatalogResponse> result =
            await service.SetStatusAsync(
                id,
                request.IsActive,
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode =
            error == ServiceCatalogErrors.NotFound
                ? StatusCodes.Status404NotFound
                : error
                    == ServiceCatalogErrors
                        .NameAlreadyExists
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }

    private static ServiceResponse Map(
        ServiceCatalogResponse response)
    {
        return new ServiceResponse(
            response.Id,
            response.Name,
            response.DefaultDurationMinutes,
            response.IsActive,
            response.CreatedAtUtc,
            response.UpdatedAtUtc);
    }
}