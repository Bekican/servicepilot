using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Services;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class ServiceCatalogItemConfiguration
    : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(
        EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.ToTable("services");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(service => service.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(service => service.Name)
            .HasColumnName("name")
            .HasMaxLength(ServiceCatalogItem.MaxNameLength)
            .IsRequired();
        builder.Property(service => service.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(ServiceCatalogItem.MaxNameLength)
            .IsRequired();
        builder.Property(service =>
                service.DefaultDurationMinutes)
            .HasColumnName("default_duration_minutes")
            .IsRequired();
        builder.Property(service => service.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(service => service.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(service => service.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
        builder.HasIndex(service => new
        {
            service.OrganizationId,
            service.NormalizedName
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_services_organization_name");
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(service =>
                service.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}