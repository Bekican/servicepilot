using ServicePilot.Application.Authorization;
using ServicePilot.Domain.Users;
using ServicePilot.UnitTests.Authentication;

namespace ServicePilot.UnitTests.Authorization;

public sealed class UserAuthorizationServiceTests
{
    public static TheoryData<
        string,
        UserCapability,
        bool> CapabilityCases => new()
    {
        {
            UserRoles.Owner,
            UserCapability.ManageUsers,
            true
        },
        {
            UserRoles.Admin,
            UserCapability.ManageUsers,
            false
        },
        {
            UserRoles.Admin,
            UserCapability.ManageServices,
            true
        },
        {
            UserRoles.Dispatcher,
            UserCapability.ManageCustomers,
            true
        },
        {
            UserRoles.Dispatcher,
            UserCapability.ManageServices,
            false
        },
        {
            UserRoles.Technician,
            UserCapability.AccessSystem,
            true
        },
        {
            UserRoles.Technician,
            UserCapability.ManageAppointments,
            false
        },
        {
            UserRoles.Admin,
            UserCapability.ManageKnowledgeDocuments,
            true
        },
        {
            UserRoles.Dispatcher,
            UserCapability.ManageKnowledgeDocuments,
            false
        },
        {
            UserRoles.Technician,
            UserCapability.UseKnowledgeAssistant,
            true
        }
    };

    [Theory]
    [MemberData(nameof(CapabilityCases))]
    public async Task HasCapability_ShouldUseCentralRoleMatrix(
        string role,
        UserCapability capability,
        bool expected)
    {
        FakeUserRepository repository = new();
        User user = CreateUser(role);
        repository.Seed(user);
        IUserAuthorizationService service =
            new UserAuthorizationService(repository);

        bool result = await service.HasCapabilityAsync(
            user.OrganizationId,
            user.Id,
            capability);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HasCapability_ShouldRejectInactiveUser()
    {
        FakeUserRepository repository = new();
        User user = CreateUser(UserRoles.Owner);
        user.Deactivate();
        repository.Seed(user);
        IUserAuthorizationService service =
            new UserAuthorizationService(repository);

        bool result = await service.HasCapabilityAsync(
            user.OrganizationId,
            user.Id,
            UserCapability.ManageUsers);

        Assert.False(result);
    }

    [Fact]
    public async Task HasCapability_ShouldRejectOtherTenant()
    {
        FakeUserRepository repository = new();
        User user = CreateUser(UserRoles.Owner);
        repository.Seed(user);
        IUserAuthorizationService service =
            new UserAuthorizationService(repository);

        bool result = await service.HasCapabilityAsync(
            Guid.NewGuid(),
            user.Id,
            UserCapability.ManageUsers);

        Assert.False(result);
    }

    private static User CreateUser(string role)
    {
        return new User(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            "User",
            $"{Guid.NewGuid():N}@example.com",
            "hashed-password",
            role,
            DateTimeOffset.UtcNow);
    }
}