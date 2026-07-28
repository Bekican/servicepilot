using Microsoft.EntityFrameworkCore;

using ServicePilot.Domain.Organizations;

namespace ServicePilot.Infrastructure.Persistence;

public sealed class ServicePilotDbContext(
    DbContextOptions<ServicePilotDbContext> options)
    : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ServicePilotDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}