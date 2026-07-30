using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Organizations;
using ServicePilot.Infrastructure.Organizations;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException(
                    "Connection string 'Database' not found.");
        services.AddDbContext<ServicePilotDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IOrganizationRepository,
            OrganizationRepository>();
        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                ServicePilotDbContext>());
        return services;

    }
}