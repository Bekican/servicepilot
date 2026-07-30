using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Organizations;
using ServicePilot.Domain.Organizations;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Organizations;

internal sealed class OrganizationRepository(
    ServicePilotDbContext dbContext)
    : IOrganizationRepository
{
    public Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.Organizations.AnyAsync(
            organization => organization.Slug == slug,
            cancellationToken
        );
    }
    public void Add(Organization organization)
    {
        dbContext.Organizations.Add(organization);
    }
}