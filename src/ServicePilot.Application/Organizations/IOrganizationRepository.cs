using ServicePilot.Domain.Organizations;

namespace ServicePilot.Application.Organizations;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Organization?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken = default);

    void Add(Organization organization);
}