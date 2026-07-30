using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Customers;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Services;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration
    : IEntityTypeConfiguration<Appointment>
{
    public void Configure(
        EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable(
            "appointments",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_appointments_time_range",
                    "end_at_utc > start_at_utc");
            });

        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(appointment =>
                appointment.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(appointment =>
                appointment.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();
        builder.Property(appointment =>
                appointment.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();
        builder.Property(appointment =>
                appointment.TechnicianUserId)
            .HasColumnName("technician_user_id");
        builder.Property(appointment =>
                appointment.StartAtUtc)
            .HasColumnName("start_at_utc")
            .IsRequired();
        builder.Property(appointment =>
                appointment.EndAtUtc)
            .HasColumnName("end_at_utc")
            .IsRequired();
        builder.Property(appointment =>
                appointment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(appointment =>
                appointment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(appointment =>
                appointment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(appointment =>
            appointment.BlocksTechnicianSlot);

        builder.HasIndex(appointment => new
        {
            appointment.OrganizationId,
            appointment.StartAtUtc
        })
            .HasDatabaseName(
                "ix_appointments_organization_start");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(appointment =>
                appointment.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(appointment => new
            {
                appointment.OrganizationId,
                appointment.CustomerId
            })
            .HasPrincipalKey(customer => new
            {
                customer.OrganizationId,
                customer.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceCatalogItem>()
            .WithMany()
            .HasForeignKey(appointment => new
            {
                appointment.OrganizationId,
                appointment.ServiceId
            })
            .HasPrincipalKey(service => new
            {
                service.OrganizationId,
                service.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                nameof(Appointment.OrganizationId),
                nameof(Appointment.TechnicianUserId))
            .HasPrincipalKey(
                nameof(User.OrganizationId),
                nameof(User.Id))
            .OnDelete(DeleteBehavior.Restrict);
    }
}