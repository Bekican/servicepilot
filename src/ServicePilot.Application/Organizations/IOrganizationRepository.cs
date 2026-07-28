using ServicePilot.Domain.Organizations;

namespace ServicePilot.Application.Organizations;

public interface IOrganizationRepository
{
    Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken = default);

    void Add(Organization organization);
}