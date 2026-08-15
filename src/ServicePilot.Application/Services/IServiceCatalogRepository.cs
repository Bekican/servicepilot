using ServicePilot.Domain.Services;

namespace ServicePilot.Application.Services;

public interface IServiceCatalogRepository
{
    Task<ServiceCatalogItem?> GetByIdAsync(
        Guid organizationId,
        Guid serviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCatalogItem>> ListAsync(
        Guid organizationId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludedServiceId,
        CancellationToken cancellationToken = default);

    Task<bool> NameBelongsToInactiveServiceAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludedServiceId,
        CancellationToken cancellationToken = default);

    void Add(ServiceCatalogItem service);
}
