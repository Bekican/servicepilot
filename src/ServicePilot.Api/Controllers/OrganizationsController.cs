using Microsoft.AspNetCore.Mvc;

using ServicePilot.Application.Common;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Contracts.Organizations;
using ServicePilot.Contracts.Organizations.CreateOrganization;



namespace ServicePilot.Api.Controllers;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(
    CreateOrganizationHandler handler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(OrganizationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        CreateOrganizationCommand command = new(
            request.Name,
            request.Slug);

        Result<CreateOrganizationResponse> result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        OrganizationResponse response = new(
            result.Value.Id,
            result.Value.Name,
            result.Value.Slug,
            result.Value.CreatedAtUtc);

        return Created(
            $"/api/organizations/{response.Id}",
            response);
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode =
            error == OrganizationErrors.SlugAlreadyExists
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}