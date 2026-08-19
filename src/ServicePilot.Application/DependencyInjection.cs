using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Appointments;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.AcceptInvitation;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Application.Authentication.Register;
using ServicePilot.Application.Authorization;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Dashboard;
using ServicePilot.Application.Email;
using ServicePilot.Application.Knowledge;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Reminders;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users.Invitations.CreateInvitation;
using ServicePilot.Application.Users.Invitations.ListInvitations;
using ServicePilot.Application.Users.Invitations.ResendInvitation;
using ServicePilot.Application.Users.Management.ChangeUserRole;
using ServicePilot.Application.Users.Management.ChangeUserStatus;
using ServicePilot.Application.Users.Management.ListUsers;
using ServicePilot.Application.Users.Technicians;

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
        services.AddScoped<CurrentSessionService>();
        services.AddScoped<CreateInvitationHandler>();
        services.AddScoped<ResendInvitationHandler>();
        services.AddScoped<ListInvitationsHandler>();
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<ChangeUserRoleHandler>();
        services.AddScoped<ChangeUserStatusHandler>();
        services.AddScoped<TechnicianDirectoryService>();
        services.AddScoped<CustomerManagementService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<
            IUserAuthorizationService,
            UserAuthorizationService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReminderManagementService>();
        services.AddScoped<ReminderProcessor>();
        services.AddScoped<EmailOutboxProcessor>();
        services.AddScoped<KnowledgeDocumentService>();
        services.AddScoped<KnowledgeAssistantService>();

        return services;
    }
}
