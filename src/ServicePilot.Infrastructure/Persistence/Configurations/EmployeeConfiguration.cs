using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Employees;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(employee => employee.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(employee => employee.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(employee => employee.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(employee => employee.Email)
.HasColumnName("email")
.HasMaxLength(320)
.IsRequired();

        builder.Property(employee => employee.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(employee => employee.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(employee => new
        {
            employee.OrganizationId,
            employee.Email
        })
        .IsUnique()
        .HasDatabaseName(
            "ux_employees_organization_id_email"
        );
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(employee => employee.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}