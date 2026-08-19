using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Knowledge;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeDocumentConfiguration
    : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable(
            "knowledge_documents",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_knowledge_documents_size_bytes",
                "size_bytes > 0"));
        builder.HasKey(document => document.Id);
        builder.HasAlternateKey(document => new
        {
            document.OrganizationId,
            document.Id
        });
        builder.Property(document => document.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(document => document.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(document => document.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();
        builder.Property(document => document.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(KnowledgeDocument.MaxOriginalFileNameLength)
            .IsRequired();
        builder.Property(document => document.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(KnowledgeDocument.MaxContentTypeLength)
            .IsRequired();
        builder.Property(document => document.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(KnowledgeDocument.MaxStorageKeyLength)
            .IsRequired();
        builder.Property(document => document.ChecksumSha256)
            .HasColumnName("checksum_sha256")
            .HasMaxLength(KnowledgeDocument.ChecksumLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(document => document.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();
        builder.Property(document => document.Type)
            .HasColumnName("document_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(document => document.AccessScope)
            .HasColumnName("access_scope")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(document => document.ProcessingAttemptCount)
            .HasColumnName("processing_attempt_count")
            .IsRequired();
        builder.Property(document => document.ProcessingStartedAtUtc)
            .HasColumnName("processing_started_at_utc");
        builder.Property(document => document.LastErrorCode)
            .HasColumnName("last_error_code")
            .HasMaxLength(KnowledgeDocument.MaxErrorCodeLength);
        builder.Property(document => document.LastErrorMessage)
            .HasColumnName("last_error_message")
            .HasMaxLength(KnowledgeDocument.MaxErrorMessageLength);
        builder.Property(document => document.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(document => document.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(document => document.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_knowledge_documents_storage_key");
        builder.HasIndex(document => new
        {
            document.OrganizationId,
            document.ChecksumSha256
        })
            .IsUnique()
            .HasFilter("status <> 'Deleted'")
            .HasDatabaseName(
                "ux_knowledge_documents_organization_checksum");
        builder.HasIndex(document => new
        {
            document.Status,
            document.CreatedAtUtc
        })
            .HasDatabaseName("ix_knowledge_documents_pending");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(document => document.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => new
            {
                document.OrganizationId,
                document.UploadedByUserId
            })
            .HasPrincipalKey(user => new
            {
                user.OrganizationId,
                user.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}