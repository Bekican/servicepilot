using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(auditLog =>
                auditLog.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(auditLog =>
                auditLog.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(auditLog => auditLog.Action)
            .HasColumnName("action")
            .HasMaxLength(AuditLog.MaxActionLength)
            .IsRequired();

        builder.Property(auditLog =>
                auditLog.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(AuditLog.MaxEntityTypeLength)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(auditLog =>
                auditLog.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(auditLog => auditLog.Metadata)
            .HasColumnName("metadata")
            .HasMaxLength(AuditLog.MaxMetadataLength);

        builder.Property(auditLog =>
                auditLog.AnonymizedAtUtc)
            .HasColumnName("anonymized_at_utc");

        builder.HasIndex(auditLog => new
        {
            auditLog.OrganizationId,
            auditLog.OccurredAtUtc
        })
            .HasDatabaseName(
                "ix_audit_logs_organization_occurred_at");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(auditLog =>
                auditLog.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(auditLog =>
                auditLog.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}