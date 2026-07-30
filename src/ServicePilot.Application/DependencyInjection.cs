using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Appointments;
using ServicePilot.Application.Authentication.AcceptInvitation;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Application.Authentication.Register;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Dashboard;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Reminders;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users.Invitations.CreateInvitation;
using ServicePilot.Application.Users.Invitations.ResendInvitation;
using ServicePilot.Application.Users.Management.ChangeUserRole;
using ServicePilot.Application.Users.Management.ChangeUserStatus;
using ServicePilot.Application.Users.Management.ListUsers;

namespace ServicePilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateOrganizationHandler>();
        services.AddScoped<RegisterOrganizationOwnerHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<AcceptInvitationHandler>();
        services.AddScoped<CreateInvitationHandler>();
        services.AddScoped<ResendInvitationHandler>();
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<ChangeUserRoleHandler>();
        services.AddScoped<ChangeUserStatusHandler>();
        services.AddScoped<CustomerManagementService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReminderManagementService>();
        services.AddScoped<ReminderProcessor>();

        return services;
    }
}