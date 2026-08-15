using Microsoft.EntityFrameworkCore;

using Npgsql;

using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Customers;
using ServicePilot.Domain.Email;
using ServicePilot.Domain.Employees;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Reminders;
using ServicePilot.Domain.Services;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Customers;

namespace ServicePilot.Infrastructure.Persistence;

public sealed class ServicePilotDbContext(
    DbContextOptions<ServicePilotDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Appointment> Appointments =>
        Set<Appointment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses =>
        Set<CustomerAddress>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ServiceCatalogItem> Services =>
        Set<ServiceCatalogItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserInvitation> UserInvitations =>
        Set<UserInvitation>();
    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();
    internal DbSet<CustomerNumberCounter>
        CustomerNumberCounters =>
            Set<CustomerNumberCounter>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyViolationException(exception);
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
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.CheckViolation
            } postgresException)
        {
            throw new ConstraintViolationException(
                postgresException.ConstraintName,
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation
            } postgresException)
        {
            throw new ConstraintViolationException(
                postgresException.ConstraintName,
                exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ServicePilotDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
