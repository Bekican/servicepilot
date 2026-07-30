using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.Register;
using ServicePilot.Domain.Users;
using ServicePilot.UnitTests.Organizations.CreateOrganization;

namespace ServicePilot.UnitTests.Authentication;

public sealed class RegisterOrganizationOwnerHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldCreateOrganizationAndOwner()
    {
        FakeOrganizationRepository organizationRepository = new();
        FakeUserRepository userRepository = new();
        FakePasswordHasher passwordHasher = new();
        FakeAccessTokenProvider accessTokenProvider = new(
            UtcNow.AddHours(1));
        FakeUnitOfWork unitOfWork = new();
        FakeTimeProvider timeProvider = new(UtcNow);

        RegisterOrganizationOwnerHandler handler = new(
            organizationRepository,
            userRepository,
            passwordHasher,
            accessTokenProvider,
            unitOfWork,
            timeProvider);

        RegisterOrganizationOwnerCommand command = new(
            " Acme Service ",
            " ACME-SERVICE ",
            " Bekir ",
            " Çakmak ",
            " BEKIR@EXAMPLE.COM ",
            "correct-password");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRoles.Owner, result.Value.Role);
        Assert.Equal(
            "token-for-" + result.Value.UserId,
            result.Value.AccessToken);

        var organization =
            Assert.Single(
                organizationRepository.Organizations);
        User owner = Assert.Single(userRepository.Users);

        Assert.Equal("Acme Service", organization.Name);
        Assert.Equal("acme-service", organization.Slug);
        Assert.Equal(organization.Id, owner.OrganizationId);
        Assert.Equal("bekir@example.com", owner.Email);
        Assert.NotEqual(
            command.Password,
            owner.PasswordHash);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenSlugAlreadyExists()
    {
        FakeOrganizationRepository organizationRepository = new();
        organizationRepository.Seed(new(
            Guid.NewGuid(),
            "Existing",
            "acme",
            UtcNow));

        FakeUserRepository userRepository = new();
        FakeUnitOfWork unitOfWork = new();

        RegisterOrganizationOwnerHandler handler = new(
            organizationRepository,
            userRepository,
            new FakePasswordHasher(),
            new FakeAccessTokenProvider(
                UtcNow.AddHours(1)),
            unitOfWork,
            new FakeTimeProvider(UtcNow));

        RegisterOrganizationOwnerCommand command = new(
            "Acme",
            "ACME",
            "Bekir",
            "Çakmak",
            "bekir@example.com",
            "correct-password");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthenticationErrors.OrganizationSlugAlreadyExists,
            result.Error);
        Assert.Empty(userRepository.Users);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPasswordIsTooShort()
    {
        FakeOrganizationRepository organizationRepository = new();
        FakeUserRepository userRepository = new();
        FakeUnitOfWork unitOfWork = new();

        RegisterOrganizationOwnerHandler handler = new(
            organizationRepository,
            userRepository,
            new FakePasswordHasher(),
            new FakeAccessTokenProvider(
                UtcNow.AddHours(1)),
            unitOfWork,
            new FakeTimeProvider(UtcNow));

        RegisterOrganizationOwnerCommand command = new(
            "Acme",
            "acme",
            "Bekir",
            "Çakmak",
            "bekir@example.com",
            "short");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthenticationErrors.PasswordTooShort,
            result.Error);
        Assert.Empty(
            organizationRepository.Organizations);
        Assert.Empty(userRepository.Users);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}