using ServicePilot.Application.Organizations;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.UnitTests.Organizations.CreateOrganization;

internal sealed class FakeOrganizationRepository
    : IOrganizationRepository
{
    private readonly List<Organization> _organizations = [];

    public IReadOnlyCollection<Organization> Organizations =>
        _organizations.AsReadOnly();

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