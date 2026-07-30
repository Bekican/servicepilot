using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Customers;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration
    : IEntityTypeConfiguration<Customer>
{
    public void Configure(
        EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(
            "customers",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_customers_type_fields",
                    """
                    (
                        type = 'Individual'
                        AND first_name IS NOT NULL
                        AND last_name IS NOT NULL
                        AND company_name IS NULL
                    )
                    OR
                    (
                        type = 'Company'
                        AND company_name IS NOT NULL
                        AND first_name IS NULL
                        AND last_name IS NULL
                    )
                    """);
            });

        builder.HasKey(customer => customer.Id);
        builder.HasAlternateKey(customer => new
        {
            customer.OrganizationId,
            customer.Id
        });

        builder.Property(customer => customer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(customer =>
                customer.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(customer => customer.Number)
            .HasColumnName("number")
            .IsRequired();
        builder.Property(customer => customer.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(customer => customer.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(Customer.MaxFirstNameLength);
        builder.Property(customer => customer.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(Customer.MaxLastNameLength);
        builder.Property(customer => customer.CompanyName)
            .HasColumnName("company_name")
            .HasMaxLength(Customer.MaxCompanyNameLength);
        builder.Property(customer =>
                customer.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(
                Customer.MaxContactPersonLength);
        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(Customer.MaxEmailLength);
        builder.Property(customer =>
                customer.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(Customer.MaxEmailLength);
        builder.Property(customer => customer.Phone)
            .HasColumnName("phone")
            .HasMaxLength(Customer.MaxPhoneLength);
        builder.Property(customer =>
                customer.NormalizedPhone)
            .HasColumnName("normalized_phone")
            .HasMaxLength(Customer.MaxPhoneLength);
        builder.Property(customer => customer.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(customer =>
                customer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(customer =>
                customer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(customer =>
            customer.CustomerNumber);

        builder.HasIndex(customer => new
        {
            customer.OrganizationId,
            customer.Number
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_customers_organization_number");
        builder.HasIndex(customer => new
        {
            customer.OrganizationId,
            customer.NormalizedEmail
        })
            .IsUnique()
            .HasFilter(
                "normalized_email IS NOT NULL")
            .HasDatabaseName(
                "ux_customers_organization_email");
        builder.HasIndex(customer => new
        {
            customer.OrganizationId,
            customer.NormalizedPhone
        })
            .IsUnique()
            .HasFilter(
                "normalized_phone IS NOT NULL")
            .HasDatabaseName(
                "ux_customers_organization_phone");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(customer =>
                customer.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}