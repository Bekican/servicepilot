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

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListPageAsync(
        Guid organizationId,
        bool includeInactive,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        string normalizedSearch = search?.Trim().ToLowerInvariant() ?? string.Empty;
        IQueryable<Customer> query = dbContext.Customers
            .AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == organizationId
                && (includeInactive || customer.IsActive));

        if (normalizedSearch.Length > 0)
        {
            string escapedSearch = normalizedSearch
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            string pattern = $"%{escapedSearch}%";
            string numberPattern = $"%{escapedSearch.Replace("cus-", string.Empty, StringComparison.Ordinal)}%";
            query = query.Where(customer =>
                EF.Functions.Like(customer.Number.ToString(), numberPattern, "\\")
                || (customer.FirstName != null
                    && EF.Functions.Like(customer.FirstName.ToLower(), pattern, "\\"))
                || (customer.LastName != null
                    && EF.Functions.Like(customer.LastName.ToLower(), pattern, "\\"))
                || (customer.CompanyName != null
                    && EF.Functions.Like(customer.CompanyName.ToLower(), pattern, "\\"))
                || (customer.NormalizedEmail != null
                    && EF.Functions.Like(customer.NormalizedEmail, pattern, "\\")));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        Customer[] items = await query.OrderBy(customer => customer.Number)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
        return (items, totalCount);
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

    public Task<bool> EmailBelongsToInactiveCustomerAsync(
        Guid organizationId,
        string normalizedEmail,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer =>
                customer.OrganizationId == organizationId
                && customer.NormalizedEmail == normalizedEmail
                && !customer.IsActive
                && (excludedCustomerId == null || customer.Id != excludedCustomerId),
            cancellationToken);
    }

    public Task<bool> PhoneBelongsToInactiveCustomerAsync(
        Guid organizationId,
        string normalizedPhone,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer =>
                customer.OrganizationId == organizationId
                && customer.NormalizedPhone == normalizedPhone
                && !customer.IsActive
                && (excludedCustomerId == null || customer.Id != excludedCustomerId),
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
        await ReloadTrackedAddressesAsync(
            organizationId,
            customerId,
            cancellationToken);
    }

    public async Task DeactivateAddressAndPromoteAsync(
        Guid organizationId,
        Guid customerId,
        Guid addressId,
        Guid? replacementAddressId,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE customer_addresses
             SET
                 is_active = CASE WHEN id = {addressId} THEN FALSE ELSE is_active END,
                 is_primary = CASE
                     WHEN id = {addressId} THEN FALSE
                     WHEN id = {replacementAddressId} THEN TRUE
                     ELSE is_primary
                 END,
                 updated_at_utc = {updatedAtUtc}
             WHERE organization_id = {organizationId}
               AND customer_id = {customerId}
               AND (id = {addressId} OR id = {replacementAddressId});
             """,
            cancellationToken);
        await ReloadTrackedAddressesAsync(
            organizationId,
            customerId,
            cancellationToken);
    }

    private async Task ReloadTrackedAddressesAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var entries = dbContext.ChangeTracker
            .Entries<CustomerAddress>()
            .Where(entry =>
                entry.Entity.OrganizationId == organizationId
                && entry.Entity.CustomerId == customerId)
            .ToArray();
        foreach (var entry in entries)
        {
            await entry.ReloadAsync(cancellationToken);
        }
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
