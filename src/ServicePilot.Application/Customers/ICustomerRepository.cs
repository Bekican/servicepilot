using ServicePilot.Domain.Customers;

namespace ServicePilot.Application.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(
        Guid organizationId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        Guid organizationId,
        string normalizedEmail,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(
        Guid organizationId,
        string normalizedPhone,
        Guid? excludedCustomerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAddress>> ListAddressesAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetAddressAsync(
        Guid organizationId,
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken = default);

    Task SetPrimaryAddressAsync(
        Guid organizationId,
        Guid customerId,
        Guid addressId,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);

    void Add(Customer customer);
    void AddAddress(CustomerAddress address);
}