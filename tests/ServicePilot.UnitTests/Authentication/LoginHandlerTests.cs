using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;
using ServicePilot.UnitTests.Organizations.CreateOrganization;

namespace ServicePilot.UnitTests.Authentication;

public sealed class LoginHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        FakePasswordHasher passwordHasher = new();
        FakeOrganizationRepository organizationRepository = new();
        FakeUserRepository userRepository = new();

        Organization organization = CreateOrganization();
        User user = CreateUser(
            organization.Id,
            passwordHasher.Hash("correct-password"));

        organizationRepository.Seed(organization);
        userRepository.Seed(user);

        LoginHandler handler = new(
            organizationRepository,
            userRepository,
            passwordHasher,
            new FakeAccessTokenProvider(
                UtcNow.AddHours(1)));

        LoginCommand command = new(
            " ACME ",
            " BEKIR@EXAMPLE.COM ",
            "correct-password");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(
            organization.Id,
            result.Value.OrganizationId);
        Assert.Equal(UserRoles.Owner, result.Value.Role);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPasswordIsWrong()
    {
        FakePasswordHasher passwordHasher = new();
        FakeOrganizationRepository organizationRepository = new();
        FakeUserRepository userRepository = new();

        Organization organization = CreateOrganization();
        User user = CreateUser(
            organization.Id,
            passwordHasher.Hash("correct-password"));

        organizationRepository.Seed(organization);
        userRepository.Seed(user);

        LoginHandler handler = new(
            organizationRepository,
            userRepository,
            passwordHasher,
            new FakeAccessTokenProvider(
                UtcNow.AddHours(1)));

        LoginCommand command = new(
            "acme",
            "bekir@example.com",
            "wrong-password");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthenticationErrors.InvalidCredentials,
            result.Error);
    }

    private static Organization CreateOrganization()
    {
        return new Organization(
            Guid.NewGuid(),
            "Acme",
            "acme",
            UtcNow);
    }

    private static User CreateUser(
        Guid organizationId,
        string passwordHash)
    {
        return new User(
            Guid.NewGuid(),
            organizationId,
            "Bekir",
            "Çakmak",
            "bekir@example.com",
            passwordHash,
            UserRoles.Owner,
            UtcNow);
    }
}