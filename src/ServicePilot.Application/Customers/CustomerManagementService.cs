using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Customers;

namespace ServicePilot.Application.Customers;

public sealed class CustomerManagementService(
    ITenantContext tenantContext,
    ICustomerRepository customerRepository,
    ICustomerNumberGenerator numberGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<CustomerResponse>> CreateAsync(
        CustomerData data,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = tenantContext.OrganizationId;
        Result<(CustomerType Type, string? Email, string? Phone)>
            validation = Normalize(data);

        if (validation.IsFailure)
        {
            return Result<CustomerResponse>.Failure(
                validation.Error);
        }

        Error? duplicateError =
            await FindDuplicateAsync(
                organizationId,
                validation.Value.Email,
                validation.Value.Phone,
                null,
                cancellationToken);

        if (duplicateError is not null)
        {
            return Result<CustomerResponse>.Failure(
                duplicateError);
        }

        long number = await numberGenerator.NextAsync(
            organizationId,
            cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        Customer customer;

        try
        {
            customer = CreateCustomer(
                data,
                validation.Value.Type,
                organizationId,
                number,
                nowUtc);
        }
        catch (ArgumentException)
        {
            return Result<CustomerResponse>.Failure(
                CustomerErrors.InvalidData);
        }

        customerRepository.Add(customer);

        Result saveResult =
            await SaveAsync(cancellationToken);

        return saveResult.IsFailure
            ? Result<CustomerResponse>.Failure(
                saveResult.Error)
            : Result<CustomerResponse>.Success(
                Map(customer, []));
    }

    public async Task<Result<CustomerResponse>> GetAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        Customer? customer =
            await customerRepository.GetByIdAsync(
                tenantContext.OrganizationId,
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return Result<CustomerResponse>.Failure(
                CustomerErrors.NotFound);
        }

        IReadOnlyList<CustomerAddress> addresses =
            await customerRepository.ListAddressesAsync(
                tenantContext.OrganizationId,
                customerId,
                cancellationToken);

        return Result<CustomerResponse>.Success(
            Map(customer, addresses));
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Customer> customers =
            await customerRepository.ListAsync(
                tenantContext.OrganizationId,
                includeInactive,
                cancellationToken);

        return customers
            .Select(customer => Map(customer, []))
            .ToArray();
    }

    public async Task<Result<CustomerResponse>> UpdateAsync(
        Guid customerId,
        CustomerData data,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = tenantContext.OrganizationId;
        Customer? customer =
            await customerRepository.GetByIdAsync(
                organizationId,
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return Result<CustomerResponse>.Failure(
                CustomerErrors.NotFound);
        }

        Result<(CustomerType Type, string? Email, string? Phone)>
            validation = Normalize(data);

        if (validation.IsFailure)
        {
            return Result<CustomerResponse>.Failure(
                validation.Error);
        }

        Error? duplicateError =
            await FindDuplicateAsync(
                organizationId,
                validation.Value.Email,
                validation.Value.Phone,
                customerId,
                cancellationToken);

        if (duplicateError is not null)
        {
            return Result<CustomerResponse>.Failure(
                duplicateError);
        }

        try
        {
            UpdateCustomer(
                customer,
                data,
                validation.Value.Type,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return Result<CustomerResponse>.Failure(
                CustomerErrors.InvalidData);
        }

        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<CustomerResponse>.Failure(
                saveResult.Error);
        }

        IReadOnlyList<CustomerAddress> addresses =
            await customerRepository.ListAddressesAsync(
                organizationId,
                customerId,
                cancellationToken);

        return Result<CustomerResponse>.Success(
            Map(customer, addresses));
    }

    public async Task<Result> DeactivateAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        Customer? customer =
            await customerRepository.GetByIdAsync(
                tenantContext.OrganizationId,
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound);
        }

        if (!customer.IsActive)
        {
            return Result.Success();
        }

        customer.Deactivate(timeProvider.GetUtcNow());
        return await SaveAsync(cancellationToken);
    }

    public async Task<Result<CustomerAddressResponse>>
        AddAddressAsync(
            Guid customerId,
            AddressData data,
            CancellationToken cancellationToken = default)
    {
        Guid organizationId = tenantContext.OrganizationId;
        Customer? customer =
            await customerRepository.GetByIdAsync(
                organizationId,
                customerId,
                cancellationToken);

        if (customer is null)
        {
            return Result<CustomerAddressResponse>.Failure(
                CustomerErrors.NotFound);
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        CustomerAddress address;

        try
        {
            address = new CustomerAddress(
                Guid.NewGuid(),
                organizationId,
                customerId,
                data.Label,
                data.Line1,
                data.Line2,
                data.City,
                data.Region,
                data.PostalCode,
                data.CountryCode,
                false,
                nowUtc);
        }
        catch (ArgumentException)
        {
            return Result<CustomerAddressResponse>.Failure(
                CustomerErrors.InvalidData);
        }

        customerRepository.AddAddress(address);
        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<CustomerAddressResponse>.Failure(
                saveResult.Error);
        }

        if (data.IsPrimary)
        {
            await customerRepository.SetPrimaryAddressAsync(
                organizationId,
                customerId,
                address.Id,
                nowUtc,
                cancellationToken);
            address.SetPrimary(true, nowUtc);
        }

        return Result<CustomerAddressResponse>.Success(
            Map(address));
    }

    public async Task<Result<CustomerAddressResponse>>
        UpdateAddressAsync(
            Guid customerId,
            Guid addressId,
            AddressData data,
            CancellationToken cancellationToken = default)
    {
        CustomerAddress? address =
            await customerRepository.GetAddressAsync(
                tenantContext.OrganizationId,
                customerId,
                addressId,
                cancellationToken);

        if (address is null)
        {
            return Result<CustomerAddressResponse>.Failure(
                CustomerErrors.AddressNotFound);
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        try
        {
            address.Update(
                data.Label,
                data.Line1,
                data.Line2,
                data.City,
                data.Region,
                data.PostalCode,
                data.CountryCode,
                nowUtc);

            if (!data.IsPrimary)
            {
                address.SetPrimary(false, nowUtc);
            }
        }
        catch (ArgumentException)
        {
            return Result<CustomerAddressResponse>.Failure(
                CustomerErrors.InvalidData);
        }

        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<CustomerAddressResponse>.Failure(
                saveResult.Error);
        }

        if (data.IsPrimary)
        {
            await customerRepository.SetPrimaryAddressAsync(
                tenantContext.OrganizationId,
                customerId,
                addressId,
                nowUtc,
                cancellationToken);
            address.SetPrimary(true, nowUtc);
        }

        return Result<CustomerAddressResponse>.Success(
            Map(address));
    }

    public async Task<Result> DeactivateAddressAsync(
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        CustomerAddress? address =
            await customerRepository.GetAddressAsync(
                tenantContext.OrganizationId,
                customerId,
                addressId,
                cancellationToken);

        if (address is null)
        {
            return Result.Failure(
                CustomerErrors.AddressNotFound);
        }

        address.Deactivate(timeProvider.GetUtcNow());
        return await SaveAsync(cancellationToken);
    }

    public async Task<Result<CustomerAddressResponse>>
        SetPrimaryAddressAsync(
            Guid customerId,
            Guid addressId,
            CancellationToken cancellationToken = default)
    {
        CustomerAddress? address =
            await customerRepository.GetAddressAsync(
                tenantContext.OrganizationId,
                customerId,
                addressId,
                cancellationToken);

        if (address is null || !address.IsActive)
        {
            return Result<CustomerAddressResponse>.Failure(
                CustomerErrors.AddressNotFound);
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        await customerRepository.SetPrimaryAddressAsync(
            tenantContext.OrganizationId,
            customerId,
            addressId,
            nowUtc,
            cancellationToken);
        address.SetPrimary(true, nowUtc);

        return Result<CustomerAddressResponse>.Success(
            Map(address));
    }

    private async Task<Error?> FindDuplicateAsync(
        Guid organizationId,
        string? normalizedEmail,
        string? normalizedPhone,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken)
    {
        if (normalizedEmail is not null
            && await customerRepository.EmailExistsAsync(
                organizationId,
                normalizedEmail,
                excludedCustomerId,
                cancellationToken))
        {
            return CustomerErrors.EmailAlreadyExists;
        }

        if (normalizedPhone is not null
            && await customerRepository.PhoneExistsAsync(
                organizationId,
                normalizedPhone,
                excludedCustomerId,
                cancellationToken))
        {
            return CustomerErrors.PhoneAlreadyExists;
        }

        return null;
    }

    private async Task<Result> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
            return Result.Success();
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_customers_organization_email")
        {
            return Result.Failure(
                CustomerErrors.EmailAlreadyExists);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_customers_organization_phone")
        {
            return Result.Failure(
                CustomerErrors.PhoneAlreadyExists);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_customer_addresses_active_primary")
        {
            return Result.Failure(
                CustomerErrors.PrimaryAddressConflict);
        }
    }

    private static Result<(
        CustomerType Type,
        string? Email,
        string? Phone)> Normalize(CustomerData data)
    {
        CustomerType type;

        if (!Enum.TryParse(
            data.Type,
            true,
            out type)
            || !Enum.IsDefined(type))
        {
            return Result<(
                CustomerType,
                string?,
                string?)>.Failure(
                    CustomerErrors.InvalidData);
        }

        try
        {
            return Result<(
                CustomerType,
                string?,
                string?)>.Success((
                    type,
                    CustomerContactNormalizer
                        .NormalizeEmail(data.Email),
                    CustomerContactNormalizer
                        .NormalizePhone(data.Phone)));
        }
        catch (ArgumentException)
        {
            return Result<(
                CustomerType,
                string?,
                string?)>.Failure(
                    CustomerErrors.InvalidData);
        }
    }

    private static Customer CreateCustomer(
        CustomerData data,
        CustomerType type,
        Guid organizationId,
        long number,
        DateTimeOffset nowUtc)
    {
        return type == CustomerType.Individual
            ? Customer.CreateIndividual(
                Guid.NewGuid(),
                organizationId,
                number,
                data.FirstName ?? string.Empty,
                data.LastName ?? string.Empty,
                data.Email,
                data.Phone,
                nowUtc)
            : Customer.CreateCompany(
                Guid.NewGuid(),
                organizationId,
                number,
                data.CompanyName ?? string.Empty,
                data.ContactPerson,
                data.Email,
                data.Phone,
                nowUtc);
    }

    private static void UpdateCustomer(
        Customer customer,
        CustomerData data,
        CustomerType type,
        DateTimeOffset nowUtc)
    {
        if (type == CustomerType.Individual)
        {
            customer.UpdateIndividual(
                data.FirstName ?? string.Empty,
                data.LastName ?? string.Empty,
                data.Email,
                data.Phone,
                nowUtc);
        }
        else
        {
            customer.UpdateCompany(
                data.CompanyName ?? string.Empty,
                data.ContactPerson,
                data.Email,
                data.Phone,
                nowUtc);
        }
    }

    private static CustomerResponse Map(
        Customer customer,
        IReadOnlyList<CustomerAddress> addresses)
    {
        return new CustomerResponse(
            customer.Id,
            customer.CustomerNumber,
            customer.Type.ToString(),
            customer.FirstName,
            customer.LastName,
            customer.CompanyName,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.IsActive,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            addresses.Select(Map).ToArray());
    }

    private static CustomerAddressResponse Map(
        CustomerAddress address)
    {
        return new CustomerAddressResponse(
            address.Id,
            address.Label,
            address.Line1,
            address.Line2,
            address.City,
            address.Region,
            address.PostalCode,
            address.CountryCode,
            address.IsPrimary,
            address.IsActive);
    }
}