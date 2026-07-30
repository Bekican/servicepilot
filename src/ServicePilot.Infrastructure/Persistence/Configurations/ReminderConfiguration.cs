using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Reminders;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class ReminderConfiguration
    : IEntityTypeConfiguration<Reminder>
{
    public void Configure(
        EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable(
            "reminders",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_reminders_attempt_count",
                    "attempt_count >= 0");
            });

        builder.HasKey(reminder => reminder.Id);
        builder.Property(reminder => reminder.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(reminder =>
                reminder.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(reminder =>
                reminder.AppointmentId)
            .HasColumnName("appointment_id")
            .IsRequired();
        builder.Property(reminder =>
                reminder.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasMaxLength(Reminder.MaxRecipientEmailLength);
        builder.Property(reminder =>
                reminder.ScheduledAtUtc)
            .HasColumnName("scheduled_at_utc")
            .IsRequired();
        builder.Property(reminder => reminder.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(reminder =>
                reminder.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();
        builder.Property(reminder =>
                reminder.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc");
        builder.Property(reminder =>
                reminder.LastAttemptAtUtc)
            .HasColumnName("last_attempt_at_utc");
        builder.Property(reminder =>
                reminder.ProcessingStartedAtUtc)
            .HasColumnName("processing_started_at_utc");
        builder.Property(reminder => reminder.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(Reminder.MaxErrorLength);
        builder.Property(reminder =>
                reminder.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(reminder =>
                reminder.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(reminder => new
        {
            reminder.Status,
            reminder.NextAttemptAtUtc
        })
            .HasDatabaseName(
                "ix_reminders_status_next_attempt");
        builder.HasIndex(reminder => new
        {
            reminder.OrganizationId,
            reminder.AppointmentId
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_reminders_organization_appointment");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(reminder =>
                reminder.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(reminder => new
            {
                reminder.OrganizationId,
                reminder.AppointmentId
            })
            .HasPrincipalKey(appointment => new
            {
                appointment.OrganizationId,
                appointment.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}