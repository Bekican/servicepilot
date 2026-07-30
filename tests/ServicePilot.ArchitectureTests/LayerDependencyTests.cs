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