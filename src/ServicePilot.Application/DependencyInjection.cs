using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Authentication.AcceptInvitation;
using ServicePilot.Application.Authentication.Login;
using ServicePilot.Application.Authentication.Register;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Users.Invitations.CreateInvitation;
using ServicePilot.Application.Users.Invitations.ResendInvitation;

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

        return services;
    }
}