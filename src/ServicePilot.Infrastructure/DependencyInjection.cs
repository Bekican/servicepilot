using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Appointments;
using ServicePilot.Application.Auditing;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Dashboard;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Reminders;
using ServicePilot.Application.Retention;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users;
using ServicePilot.Infrastructure.Appointments;
using ServicePilot.Infrastructure.Auditing;
using ServicePilot.Infrastructure.Authentication;
using ServicePilot.Infrastructure.Customers;
using ServicePilot.Infrastructure.Dashboard;
using ServicePilot.Infrastructure.Email;
using ServicePilot.Infrastructure.Organizations;
using ServicePilot.Infrastructure.Persistence;
using ServicePilot.Infrastructure.Reminders;
using ServicePilot.Infrastructure.Retention;
using ServicePilot.Infrastructure.Services;
using ServicePilot.Infrastructure.Users;

namespace ServicePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        services.AddScoped<IAppointmentRepository,
            AppointmentRepository>();
        services.AddScoped<IReminderRepository,
            ReminderRepository>();
        services.AddScoped<IRetentionService,
            RetentionService>();
        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    ServicePilotDbContext>());
        services.AddSingleton(
            _ => SmtpOptions.FromConfiguration(
                configuration));
        services.AddSingleton<IEmailSender,
            SmtpEmailSender>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);

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
        services.AddScoped<IAppointmentRepository,
            AppointmentRepository>();
        services.AddScoped<IReminderRepository,
            ReminderRepository>();
        services.AddScoped<IDashboardRepository,
            DashboardRepository>();
        services.AddScoped<IRetentionService,
            RetentionService>();
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

    private static void AddDatabase(
        IServiceCollection services,
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
    }
}