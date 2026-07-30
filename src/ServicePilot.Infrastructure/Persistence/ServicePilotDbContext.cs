using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Domain.Organizations;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Persistence;

public sealed class ServicePilotDbContext(
    DbContextOptions<ServicePilotDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ServicePilotDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}