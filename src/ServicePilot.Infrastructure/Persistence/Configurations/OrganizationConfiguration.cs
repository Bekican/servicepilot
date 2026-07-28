using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Organizations;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration
    : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(organization => organization.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(organization => organization.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(organization => organization.Slug)
            .IsUnique()
            .HasDatabaseName("ux_organizations_slug");
    }
}