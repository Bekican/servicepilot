using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Services;
using ServicePilot.Domain.Services;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Services;

internal sealed class ServiceCatalogRepository(
    ServicePilotDbContext dbContext)
    : IServiceCatalogRepository
{
    public Task<ServiceCatalogItem?> GetByIdAsync(
        Guid organizationId,
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Services.SingleOrDefaultAsync(
            service =>
                service.OrganizationId == organizationId
                && service.Id == serviceId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCatalogItem>>
        ListAsync(
            Guid organizationId,
            bool includeInactive,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Services
            .AsNoTracking()
            .Where(service =>
                service.OrganizationId == organizationId
                && (
                    includeInactive
                    || service.IsActive
                ))
            .OrderBy(service => service.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludedServiceId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Services.AnyAsync(
            service =>
                service.OrganizationId == organizationId
                && service.NormalizedName == normalizedName
                && (
                    excludedServiceId == null
                    || service.Id != excludedServiceId
                ),
            cancellationToken);
    }

    public Task<bool> NameBelongsToInactiveServiceAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludedServiceId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Services.AnyAsync(
            service =>
                service.OrganizationId == organizationId
                && service.NormalizedName == normalizedName
                && !service.IsActive
                && (excludedServiceId == null || service.Id != excludedServiceId),
            cancellationToken);
    }

    public void Add(ServiceCatalogItem service)
    {
        dbContext.Services.Add(service);
    }
}
