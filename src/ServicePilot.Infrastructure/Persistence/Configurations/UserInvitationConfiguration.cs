using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class UserInvitationConfiguration
    : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(
        EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("user_invitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(invitation =>
                invitation.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(invitation => invitation.Email)
            .HasColumnName("email")
            .HasMaxLength(UserInvitation.MaxEmailLength)
            .IsRequired();

        builder.Property(invitation => invitation.Role)
            .HasColumnName("role")
            .HasMaxLength(User.MaxRoleLength)
            .IsRequired();

        builder.Property(invitation =>
                invitation.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(UserInvitation.TokenHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(invitation => invitation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(invitation =>
                invitation.IssuedAtUtc)
            .HasColumnName("issued_at_utc")
            .IsRequired();

        builder.Property(invitation =>
                invitation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(invitation =>
                invitation.AcceptedAtUtc)
            .HasColumnName("accepted_at_utc");

        builder.Property(invitation =>
                invitation.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.HasIndex(invitation =>
                invitation.TokenHash)
            .IsUnique()
            .HasDatabaseName(
                "ux_user_invitations_token_hash");

        builder.HasIndex(invitation => new
        {
            invitation.OrganizationId,
            invitation.Email
        })
            .IsUnique()
            .HasFilter("status = 'Pending'")
            .HasDatabaseName(
                "ux_user_invitations_pending_email");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(invitation =>
                invitation.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}