using ServicePilot.Application.Appointments;
using ServicePilot.Domain.Organizations;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotDependOnOtherServicePilotLayers()
    {
        string[] references =
            GetServicePilotReferences(
                typeof(Organization).Assembly);

        Assert.Empty(references);
    }

    [Fact]
    public void Application_ShouldDependOnlyOnDomain()
    {
        string[] references =
            GetServicePilotReferences(
                typeof(AppointmentService).Assembly);

        Assert.Equal(
            ["ServicePilot.Domain"],
            references);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        string[] references =
            GetServicePilotReferences(
                typeof(ServicePilotDbContext).Assembly);

        Assert.DoesNotContain(
            "ServicePilot.Api",
            references);
    }

    [Fact]
    public void Api_ShouldNotDependOnDomain()
    {
        string[] references =
            GetServicePilotReferences(
                typeof(Program).Assembly);

        Assert.DoesNotContain(
            "ServicePilot.Domain",
            references);
    }

    [Fact]
    public void ApiControllers_ShouldNotUseInfrastructureTypes()
    {
        Type[] controllerTypes =
            typeof(Program).Assembly
                .GetTypes()
                .Where(type =>
                    type.Namespace
                        == "ServicePilot.Api.Controllers")
                .ToArray();

        string[] infrastructureDependencies =
            controllerTypes
                .SelectMany(type =>
                    type.GetConstructors())
                .SelectMany(constructor =>
                    constructor.GetParameters())
                .Select(parameter =>
                    parameter.ParameterType.Assembly
                        .GetName().Name)
                .Where(name =>
                    name
                        == "ServicePilot.Infrastructure")
                .Cast<string>()
                .ToArray();

        Assert.Empty(infrastructureDependencies);
    }

    private static string[] GetServicePilotReferences(
        System.Reflection.Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name =>
                name?.StartsWith(
                    "ServicePilot.",
                    StringComparison.Ordinal)
                == true)
            .Cast<string>()
            .Order()
            .ToArray();
    }
}