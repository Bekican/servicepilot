using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

using ServicePilot.Api.Authentication;
using ServicePilot.Application;
using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

JwtOptions jwtOptions =
    JwtOptions.FromConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.ActiveOwner,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveOwnerRequirement());
        });
    options.AddPolicy(
        AuthorizationPolicies.ActiveUser,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveRoleRequirement());
        });
    options.AddPolicy(
        AuthorizationPolicies.CustomerWrite,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveRoleRequirement(
                    UserRoles.Owner,
                    UserRoles.Admin,
                    UserRoles.Dispatcher));
        });
});
builder.Services.AddScoped<
    IAuthorizationHandler,
    ActiveOwnerAuthorizationHandler>();
builder.Services.AddScoped<
    IAuthorizationHandler,
    ActiveRoleAuthorizationHandler>();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<ITenantContext>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            HttpTenantContext>());
builder.Services.AddScoped<ICurrentUserContext>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            HttpTenantContext>());

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType =
                JwtRegisteredClaimNames.Sub,
            RoleClaimType =
                AuthenticationClaimNames.Role
        };
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;