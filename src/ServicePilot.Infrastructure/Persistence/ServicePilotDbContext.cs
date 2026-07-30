using Microsoft.EntityFrameworkCore;

using Npgsql;

using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.Infrastructure.Persistence;

public sealed class ServicePilotDbContext(
    DbContextOptions<ServicePilotDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } postgresException)
        {
            throw new UniqueConstraintViolationException(
                postgresException.ConstraintName,
                exception
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ServicePilotDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}