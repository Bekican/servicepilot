using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Auditing;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users;
using ServicePilot.Infrastructure.Auditing;
using ServicePilot.Infrastructure.Authentication;
using ServicePilot.Infrastructure.Customers;
using ServicePilot.Infrastructure.Email;
using ServicePilot.Infrastructure.Organizations;
using ServicePilot.Infrastructure.Persistence;
using ServicePilot.Infrastructure.Services;
using ServicePilot.Infrastructure.Users;

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
        services.AddScoped<IUserRepository,
            UserRepository>();
        services.AddScoped<IUserInvitationRepository,
            UserInvitationRepository>();
        services.AddScoped<IAuditLogRepository,
            AuditLogRepository>();
        services.AddScoped<ICustomerNumberGenerator,
            CustomerNumberGenerator>();
        services.AddScoped<ICustomerRepository,
            CustomerRepository>();
        services.AddScoped<IServiceCatalogRepository,
            ServiceCatalogRepository>();
        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                ServicePilotDbContext>());
        services.AddSingleton<IPasswordHasher,
            AspNetPasswordHasher>();
        services.AddSingleton<IAccessTokenProvider,
            JwtAccessTokenProvider>();
        services.AddSingleton<IInvitationTokenService,
            InvitationTokenService>();
        services.AddSingleton(
            serviceProvider =>
                SmtpOptions.FromConfiguration(
                    configuration));
        services.AddSingleton<IEmailSender,
            SmtpEmailSender>();
        services.AddSingleton(
            serviceProvider =>
                InvitationLinkOptions.FromConfiguration(
                    configuration));
        services.AddSingleton<IInvitationLinkBuilder,
            InvitationLinkBuilder>();
        return services;

    }
}