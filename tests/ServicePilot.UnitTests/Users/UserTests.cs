using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldCreateActiveOwner_WhenValuesAreValid()
    {
        Guid userId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        User user = new(
            userId,
            organizationId,
            " Bekir ",
            " Çakmak ",
            " BEKIR@EXAMPLE.COM ",
            "hashed-password",
            UserRoles.Owner,
            UtcNow);

        Assert.Equal(userId, user.Id);
        Assert.Equal(organizationId, user.OrganizationId);
        Assert.Equal("Bekir", user.FirstName);
        Assert.Equal("Çakmak", user.LastName);
        Assert.Equal("bekir@example.com", user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.Equal(UserRoles.Owner, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(UtcNow, user.CreatedAtUtc);
    }

    [Fact]
    public void Deactivate_ShouldMakeUserInactive()
    {
        User user = CreateUser();

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeUserActive()
    {
        User user = CreateUser();
        user.Deactivate();

        user.Activate();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateSupportedRole()
    {
        User user = CreateUser();

        user.ChangeRole(UserRoles.Dispatcher);

        Assert.Equal(UserRoles.Dispatcher, user.Role);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRoleIsUnsupported()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Bekir",
                "Çakmak",
                "bekir@example.com",
                "hashed-password",
                "SuperUser",
                UtcNow));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenFirstNameIsTooLong()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new string(
                    'a',
                    User.MaxFirstNameLength + 1),
                "Çakmak",
                "bekir@example.com",
                "hashed-password",
                UserRoles.Owner,
                UtcNow));
    }

    private static User CreateUser()
    {
        return new User(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Bekir",
            "Çakmak",
            "bekir@example.com",
            "hashed-password",
            UserRoles.Owner,
            UtcNow);
    }
}