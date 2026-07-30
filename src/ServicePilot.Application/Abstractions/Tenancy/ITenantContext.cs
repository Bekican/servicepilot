namespace ServicePilot.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid OrganizationId { get; }
}