using ServicePilot.Application.Organizations;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.UnitTests.Organizations.CreateOrganization;

internal sealed class FakeOrganizationRepository
    : IOrganizationRepository
{
    private readonly List<Organization> _organizations = [];

    public IReadOnlyCollection<Organization> Organizations =>
        _organizations.AsReadOnly();

    public Task<Organization?> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        Organization? organization =
            _organizations.SingleOrDefault(
                candidate =>
                    candidate.Id == organizationId);

        return Task.FromResult(organization);
    }

    public Task<Organization?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        Organization? organization =
            _organizations.SingleOrDefault(
                candidate => candidate.Slug == slug);

        return Task.FromResult(organization);
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        bool exists = _organizations.Any(
            organization => organization.Slug == slug);

        return Task.FromResult(exists);
    }

    public void Add(Organization organization)
    {
        _organizations.Add(organization);
    }

    public void Seed(Organization organization)
    {
        _organizations.Add(organization);
    }
}