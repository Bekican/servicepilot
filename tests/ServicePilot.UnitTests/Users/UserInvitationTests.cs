using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Users;

public sealed class UserInvitationTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldCreatePendingInvitation()
    {
        UserInvitation invitation =
            CreateInvitation();

        Assert.Equal(
            UserInvitationStatus.Pending,
            invitation.Status);
        Assert.True(invitation.CanBeAcceptedAt(
            UtcNow.AddHours(1)));
        Assert.False(invitation.CanBeAcceptedAt(
            UtcNow.AddHours(24)));
    }

    [Fact]
    public void Renew_ShouldReplaceTokenAndExtendExpiration()
    {
        UserInvitation invitation =
            CreateInvitation();
        string replacementHash = new('b', 64);

        invitation.Renew(
            UserRoles.Admin,
            replacementHash,
            UtcNow.AddHours(25),
            UtcNow.AddHours(49));

        Assert.Equal(replacementHash, invitation.TokenHash);
        Assert.Equal(UserRoles.Admin, invitation.Role);
        Assert.Equal(
            UtcNow.AddHours(49),
            invitation.ExpiresAtUtc);
    }

    [Fact]
    public void MarkAccepted_ShouldMakeTokenSingleUse()
    {
        UserInvitation invitation =
            CreateInvitation();

        invitation.MarkAccepted(UtcNow.AddHours(1));

        Assert.Equal(
            UserInvitationStatus.Accepted,
            invitation.Status);
        Assert.False(invitation.CanBeAcceptedAt(
            UtcNow.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() =>
            invitation.MarkAccepted(UtcNow.AddHours(2)));
    }

    [Fact]
    public void Revoke_ShouldPreventAcceptance()
    {
        UserInvitation invitation =
            CreateInvitation();

        invitation.Revoke(UtcNow.AddMinutes(1));

        Assert.Equal(
            UserInvitationStatus.Revoked,
            invitation.Status);
        Assert.False(invitation.CanBeAcceptedAt(
            UtcNow.AddMinutes(2)));
    }

    private static UserInvitation CreateInvitation()
    {
        return new UserInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " INVITED@EXAMPLE.COM ",
            UserRoles.Technician,
            new string('a', 64),
            UtcNow,
            UtcNow.AddHours(24));
    }
}