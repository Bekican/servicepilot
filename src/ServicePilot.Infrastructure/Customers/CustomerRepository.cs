using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Customers;
using ServicePilot.Domain.Customers;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Customers;

internal sealed class CustomerRepository(
    ServicePilotDbContext dbContext)
    : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.SingleOrDefaultAsync(
            customer =>
                customer.OrganizationId == organizationId
                && customer.Id == customerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(
        Guid organizationId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == organizationId
                && (
                    includeInactive
                    || customer.IsActive
                ))
            .OrderBy(customer => customer.Number)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        Guid organizationId,
        string normalizedEmail,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer =>
                customer.OrganizationId == organizationId
                && customer.NormalizedEmail
                    == normalizedEmail
                && (
                    excludedCustomerId == null
                    || customer.Id != excludedCustomerId
                ),
            cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(
        Guid organizationId,
        string normalizedPhone,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer =>
                customer.OrganizationId == organizationId
                && customer.NormalizedPhone
                    == normalizedPhone
                && (
                    excludedCustomerId == null
                    || customer.Id != excludedCustomerId
                ),
            cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerAddress>>
        ListAddressesAsync(
            Guid organizationId,
            Guid customerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerAddresses
            .Where(address =>
                address.OrganizationId == organizationId
                && address.CustomerId == customerId)
            .OrderByDescending(address =>
                address.IsPrimary)
            .ThenBy(address => address.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CustomerAddress?> GetAddressAsync(
        Guid organizationId,
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CustomerAddresses
            .SingleOrDefaultAsync(
                address =>
                    address.OrganizationId
                        == organizationId
                    && address.CustomerId == customerId
                    && address.Id == addressId,
                cancellationToken);
    }

    public async Task SetPrimaryAddressAsync(
        Guid organizationId,
        Guid customerId,
        Guid addressId,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE customer_addresses
             SET
                 is_primary =
                     CASE
                         WHEN id = {addressId} THEN TRUE
                         ELSE FALSE
                     END,
                 updated_at_utc = {updatedAtUtc}
             WHERE organization_id = {organizationId}
               AND customer_id = {customerId}
               AND is_active = TRUE
               AND (
                   is_primary = TRUE
                   OR id = {addressId}
               );
             """,
            cancellationToken);
    }

    public void Add(Customer customer)
    {
        dbContext.Customers.Add(customer);
    }

    public void AddAddress(CustomerAddress address)
    {
        dbContext.CustomerAddresses.Add(address);
    }
}