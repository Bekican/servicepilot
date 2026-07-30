namespace ServicePilot.Infrastructure.Customers;

internal sealed class CustomerNumberCounter
{
    public Guid OrganizationId { get; set; }
    public long NextValue { get; set; }
}