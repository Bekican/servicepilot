using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Domain.Organizations;

namespace ServicePilot.UnitTests.Organizations.CreateOrganization;

public sealed class CreateOrganizationHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldCreateOrganization_WhenCommandIsValid()
    {
        FakeOrganizationRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        FakeTimeProvider timeProvider = new(UtcNow);

        CreateOrganizationHandler handler = new(
            repository,
            unitOfWork,
            timeProvider);

        CreateOrganizationCommand command = new(
            " Acme Technical Service ",
            " ACME-TECHNICAL-SERVICE ");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Technical Service", result.Value.Name);
        Assert.Equal("acme-technical-service", result.Value.Slug);
        Assert.Equal(UtcNow, result.Value.CreatedAtUtc);

        Assert.Single(repository.Organizations);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenDatabaseSlugConstraintIsViolated()
    {
        FakeOrganizationRepository repository = new();

        FakeUnitOfWork unitOfWork = new()
        {
            ExceptionToThrow =
                new UniqueConstraintViolationException(
                    "ux_organizations_slug",
                    new InvalidOperationException())
        };

        FakeTimeProvider timeProvider = new(UtcNow);

        CreateOrganizationHandler handler = new(
            repository,
            unitOfWork,
            timeProvider);

        CreateOrganizationCommand command = new(
            "Acme Technical Service",
            "acme");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationErrors.SlugAlreadyExists,
            result.Error);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenSlugAlreadyExists()
    {
        FakeOrganizationRepository repository = new();

        Organization existingOrganization = new(
            Guid.NewGuid(),
            "Existing Organization",
            "acme",
            UtcNow);

        repository.Seed(existingOrganization);

        FakeUnitOfWork unitOfWork = new();
        FakeTimeProvider timeProvider = new(UtcNow);

        CreateOrganizationHandler handler = new(
            repository,
            unitOfWork,
            timeProvider);

        CreateOrganizationCommand command = new(
            "New Organization",
            "ACME");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationErrors.SlugAlreadyExists,
            result.Error);

        Assert.Single(repository.Organizations);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData("acme service")]
    [InlineData("acme_service")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("acme--service")]
    public async Task HandleAsync_ShouldFail_WhenSlugIsInvalid(
        string slug)
    {
        FakeOrganizationRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        FakeTimeProvider timeProvider = new(UtcNow);

        CreateOrganizationHandler handler = new(
            repository,
            unitOfWork,
            timeProvider);

        CreateOrganizationCommand command = new(
            "Acme",
            slug);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationErrors.InvalidSlug,
            result.Error);

        Assert.Empty(repository.Organizations);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}