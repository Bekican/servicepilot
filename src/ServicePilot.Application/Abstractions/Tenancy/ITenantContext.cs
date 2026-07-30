namespace ServicePilot.Abstraction.Tenancy;

public interface ITenantContext
{
    Guid OrganizationId { get; }
}