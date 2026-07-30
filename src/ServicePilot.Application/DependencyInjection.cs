using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Organizations.CreateOrganization;

namespace ServicePilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateOrganizationHandler>();

        return services;
    }
}