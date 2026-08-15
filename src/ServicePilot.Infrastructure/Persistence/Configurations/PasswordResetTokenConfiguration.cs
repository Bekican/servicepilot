using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration
    : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(token => token.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(token => token.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(token => token.UsedAtUtc).HasColumnName("used_at_utc");
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_password_reset_tokens_hash");
        builder.HasIndex(token => new { token.OrganizationId, token.UserId });
        builder.HasOne<User>().WithMany().HasForeignKey(token => new { token.OrganizationId, token.UserId })
            .HasPrincipalKey(user => new { user.OrganizationId, user.Id }).OnDelete(DeleteBehavior.Cascade);
    }
}