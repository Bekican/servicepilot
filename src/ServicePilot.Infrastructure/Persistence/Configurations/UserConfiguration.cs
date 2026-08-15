using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.HasAlternateKey(user => new
        {
            user.OrganizationId,
            user.Id
        });

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(user => user.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(user => user.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(User.MaxFirstNameLength)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(User.MaxLastNameLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(User.MaxEmailLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(User.MaxPasswordHashLength)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasMaxLength(User.MaxRoleLength)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(user => user.SessionVersion)
            .HasColumnName("session_version")
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(user => new
        {
            user.OrganizationId,
            user.Email
        })
        .IsUnique()
        .HasDatabaseName(
            "ux_users_organization_id_email");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}