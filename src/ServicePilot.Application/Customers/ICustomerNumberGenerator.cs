namespace ServicePilot.Application.Customers;

public interface ICustomerNumberGenerator
{
    Task<long> NextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}