using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Organizations;
using ServicePilot.Infrastructure.Customers;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class CustomerNumberCounterConfiguration
    : IEntityTypeConfiguration<CustomerNumberCounter>
{
    public void Configure(
        EntityTypeBuilder<CustomerNumberCounter> builder)
    {
        builder.ToTable("customer_number_counters");

        builder.HasKey(counter =>
            counter.OrganizationId);
        builder.Property(counter =>
                counter.OrganizationId)
            .HasColumnName("organization_id")
            .ValueGeneratedNever();
        builder.Property(counter => counter.NextValue)
            .HasColumnName("next_value")
            .IsRequired();

        builder.HasOne<Organization>()
            .WithOne()
            .HasForeignKey<CustomerNumberCounter>(
                counter =>
                    counter.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}