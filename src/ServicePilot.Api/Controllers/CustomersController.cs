using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Application.Customers;
using ServicePilot.Contracts.Customers;

using ApplicationAddressResponse =
    ServicePilot.Application.Customers.CustomerAddressResponse;
using ApplicationCustomerResponse =
    ServicePilot.Application.Customers.CustomerResponse;
using ContractAddressResponse =
    ServicePilot.Contracts.Customers.CustomerAddressResponse;
using ContractCustomerResponse =
    ServicePilot.Contracts.Customers.CustomerResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route("api/customers")]
public sealed class CustomersController(
    CustomerManagementService service)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    [ProducesResponseType(
        typeof(ContractCustomerResponse),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CustomerUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationCustomerResponse> result =
            await service.CreateAsync(
                MapData(request),
                cancellationToken);

        return result.IsSuccess
            ? Created(
                $"/api/customers/{result.Value.Id}",
                Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ContractCustomerResponse>>> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationCustomerResponse> customers =
            await service.ListAsync(
                includeInactive,
                cancellationToken);

        return Ok(customers.Select(Map));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ContractCustomerResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<ApplicationCustomerResponse> result =
            await service.GetAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    [ProducesResponseType(
        typeof(ContractCustomerResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        CustomerUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationCustomerResponse> result =
            await service.UpdateAsync(
                id,
                MapData(request),
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result result =
            await service.DeactivateAsync(
                id,
                cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [HttpPost("{customerId:guid}/addresses")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    [ProducesResponseType(
        typeof(ContractAddressResponse),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> AddAddress(
        Guid customerId,
        CustomerAddressUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationAddressResponse> result =
            await service.AddAddressAsync(
                customerId,
                MapData(request),
                cancellationToken);

        return result.IsSuccess
            ? Created(
                $"/api/customers/{customerId}/addresses/{result.Value.Id}",
                Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPut(
        "{customerId:guid}/addresses/{addressId:guid}")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    [ProducesResponseType(
        typeof(ContractAddressResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAddress(
        Guid customerId,
        Guid addressId,
        CustomerAddressUpsertRequest request,
        CancellationToken cancellationToken)
    {
        Result<ApplicationAddressResponse> result =
            await service.UpdateAddressAsync(
                customerId,
                addressId,
                MapData(request),
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPost(
        "{customerId:guid}/addresses/{addressId:guid}/deactivate")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    public async Task<IActionResult> DeactivateAddress(
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        Result result =
            await service.DeactivateAddressAsync(
                customerId,
                addressId,
                cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    [HttpPut(
        "{customerId:guid}/addresses/{addressId:guid}/primary")]
    [Authorize(Policy =
        AuthorizationPolicies.CustomerWrite)]
    [ProducesResponseType(
        typeof(ContractAddressResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPrimaryAddress(
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        Result<ApplicationAddressResponse> result =
            await service.SetPrimaryAddressAsync(
                customerId,
                addressId,
                cancellationToken);

        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode =
            error == CustomerErrors.NotFound
                || error == CustomerErrors.AddressNotFound
                ? StatusCodes.Status404NotFound
                : error == CustomerErrors.EmailAlreadyExists
                    || error == CustomerErrors.PhoneAlreadyExists
                    || error
                        == CustomerErrors
                            .PrimaryAddressConflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }

    private static CustomerData MapData(
        CustomerUpsertRequest request)
    {
        return new CustomerData(
            request.Type,
            request.FirstName,
            request.LastName,
            request.CompanyName,
            request.ContactPerson,
            request.Email,
            request.Phone);
    }

    private static AddressData MapData(
        CustomerAddressUpsertRequest request)
    {
        return new AddressData(
            request.Label,
            request.Line1,
            request.Line2,
            request.City,
            request.Region,
            request.PostalCode,
            request.CountryCode,
            request.IsPrimary);
    }

    private static ContractCustomerResponse Map(
        ApplicationCustomerResponse response)
    {
        return new ContractCustomerResponse(
            response.Id,
            response.CustomerNumber,
            response.Type,
            response.FirstName,
            response.LastName,
            response.CompanyName,
            response.ContactPerson,
            response.Email,
            response.Phone,
            response.IsActive,
            response.CreatedAtUtc,
            response.UpdatedAtUtc,
            response.Addresses.Select(Map).ToArray());
    }

    private static ContractAddressResponse Map(
        ApplicationAddressResponse response)
    {
        return new ContractAddressResponse(
            response.Id,
            response.Label,
            response.Line1,
            response.Line2,
            response.City,
            response.Region,
            response.PostalCode,
            response.CountryCode,
            response.IsPrimary,
            response.IsActive);
    }
}