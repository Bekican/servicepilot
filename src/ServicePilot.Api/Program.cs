using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using ServicePilot.Api.Authentication;
using ServicePilot.Application;
using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Authentication;
using ServicePilot.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

JwtOptions jwtOptions =
    JwtOptions.FromConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        "authentication",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                GetClientPartition(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit =
                        builder.Configuration.GetValue(
                            "RateLimiting:"
                            + "AuthenticationPermitLimit",
                            60),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    options.AddPolicy(
        "invitations",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                GetClientPartition(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit =
                        builder.Configuration.GetValue(
                            "RateLimiting:"
                            + "InvitationPermitLimit",
                            30),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
});
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
    options.AddPolicy(
        AuthorizationPolicies.ServiceWrite,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveRoleRequirement(
                    UserRoles.Owner,
                    UserRoles.Admin));
        });
    options.AddPolicy(
        AuthorizationPolicies.AppointmentManage,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveRoleRequirement(
                    UserRoles.Owner,
                    UserRoles.Admin,
                    UserRoles.Dispatcher));
        });
    options.AddPolicy(
        AuthorizationPolicies.DashboardView,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new ActiveRoleRequirement(
                    UserRoles.Owner,
                    UserRoles.Admin));
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

if (builder.Configuration.GetValue<bool>(
    "Database:MigrateOnStartup"))
{
    await using AsyncServiceScope scope =
        app.Services.CreateAsyncScope();
    ServicePilotDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<
            ServicePilotDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpLogging();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions =
        "nosniff";
    context.Response.Headers.XFrameOptions =
        "DENY";
    context.Response.Headers.Append(
        "Referrer-Policy",
        "no-referrer");
    await next(context);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string GetClientPartition(
    HttpContext httpContext)
{
    return httpContext.Connection.RemoteIpAddress?
        .ToString()
        ?? "unknown";
}

public partial class Program;