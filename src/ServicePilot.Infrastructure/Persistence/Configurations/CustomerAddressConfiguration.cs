using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Customers;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAddressConfiguration
    : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(
        EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(address =>
                address.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(address => address.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();
        builder.Property(address => address.Label)
            .HasColumnName("label")
            .HasMaxLength(
                CustomerAddress.MaxLabelLength);
        builder.Property(address => address.Line1)
            .HasColumnName("line1")
            .HasMaxLength(
                CustomerAddress.MaxLineLength)
            .IsRequired();
        builder.Property(address => address.Line2)
            .HasColumnName("line2")
            .HasMaxLength(
                CustomerAddress.MaxLineLength);
        builder.Property(address => address.City)
            .HasColumnName("city")
            .HasMaxLength(
                CustomerAddress.MaxCityLength)
            .IsRequired();
        builder.Property(address => address.Region)
            .HasColumnName("region")
            .HasMaxLength(
                CustomerAddress.MaxRegionLength);
        builder.Property(address => address.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(
                CustomerAddress.MaxPostalCodeLength);
        builder.Property(address =>
                address.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(
                CustomerAddress.CountryCodeLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(address => address.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();
        builder.Property(address => address.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(address =>
                address.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(address =>
                address.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(address => new
        {
            address.OrganizationId,
            address.CustomerId
        })
            .IsUnique()
            .HasFilter(
                "is_primary = TRUE AND is_active = TRUE")
            .HasDatabaseName(
                "ux_customer_addresses_active_primary");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(address => new
            {
                address.OrganizationId,
                address.CustomerId
            })
            .HasPrincipalKey(customer => new
            {
                customer.OrganizationId,
                customer.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}