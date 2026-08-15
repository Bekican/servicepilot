using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicePilot.Domain.Email;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class EmailOutboxMessageConfiguration
    : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("email_outbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(message => message.Recipient).HasColumnName("recipient").HasMaxLength(320).IsRequired();
        builder.Property(message => message.Subject).HasColumnName("subject").HasMaxLength(200).IsRequired();
        builder.Property(message => message.Body).HasColumnName("body").HasMaxLength(4000);
        builder.Property(message => message.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(message => message.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(message => message.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        builder.Property(message => message.ProcessingStartedAtUtc).HasColumnName("processing_started_at_utc");
        builder.Property(message => message.SentAtUtc).HasColumnName("sent_at_utc");
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(500);
        builder.Property(message => message.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(message => message.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(message => new { message.Status, message.NextAttemptAtUtc })
            .HasDatabaseName("ix_email_outbox_due");
    }
}
