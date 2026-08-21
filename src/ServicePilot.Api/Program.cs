using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Trace;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Api.Health;
using ServicePilot.Api.Observability;
using ServicePilot.Application;
using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authorization;
using ServicePilot.Application.Users;
using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Authentication;
using ServicePilot.Infrastructure.Persistence;
using ServicePilot.Observability;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
    LogLevel.None);

builder.AddServicePilotObservability(
    "ServicePilot.Api",
    tracing => tracing.AddAspNetCoreInstrumentation(
        options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments(
                    "/health");
        }));

JwtOptions jwtOptions =
    JwtOptions.FromConfiguration(builder.Configuration);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory =
            actionContext =>
            {
                Dictionary<string, string[]> errors =
                    actionContext.ModelState
                        .Where(pair =>
                            pair.Value?.Errors.Count > 0)
                        .ToDictionary(
                            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
                            pair => pair.Value!.Errors
                                .Select(_ =>
                                    "Invalid")
                                .Distinct(
                                    StringComparer.Ordinal)
                                .ToArray(),
                            StringComparer.Ordinal);
                ApiProblemDetailsFactory factory =
                    actionContext.HttpContext
                        .RequestServices
                        .GetRequiredService<
                            ApiProblemDetailsFactory>();
                BadRequestObjectResult result = new(
                    factory.CreateValidation(
                        actionContext.HttpContext,
                        errors));
                result.ContentTypes.Add(
                    "application/problem+json");
                return result;
            };
    });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<ApiProblemDetailsFactory>();
builder.Services.AddSingleton<PasswordResetRequestLimiter>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (IConfigurationSection networkSection in builder.Configuration
        .GetSection("ForwardedHeaders:KnownNetworks")
        .GetChildren())
    {
        if (System.Net.IPNetwork.TryParse(
            networkSection.Value,
            out System.Net.IPNetwork network))
        {
            options.KnownIPNetworks.Add(network);
        }
    }
});
builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>(
        "database",
        tags: ["ready"])
    .AddCheck<KnowledgeReadinessHealthCheck>(
        "knowledge",
        tags: ["knowledge"]);
builder.Services.AddHttpClient(
    "knowledge-health",
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["KnowledgeAi:OllamaBaseUrl"]
                ?? "http://localhost:11434");
        client.Timeout = TimeSpan.FromSeconds(5);
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (
        context,
        cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out TimeSpan retryAfter))
        {
            int retryAfterSeconds = Math.Max(
                1,
                (int)Math.Ceiling(
                    retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers
                .RetryAfter = retryAfterSeconds.ToString(
                    CultureInfo.InvariantCulture);
        }

        ApiProblemDetailsFactory factory =
            context.HttpContext.RequestServices
                .GetRequiredService<
                    ApiProblemDetailsFactory>();
        await factory.WriteAsync(
            context.HttpContext,
            ApiProblemCodes.RateLimitExceeded,
            StatusCodes.Status429TooManyRequests,
            "Too many requests. Try again later.",
            cancellationToken);
    };
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
    options.AddPolicy(
        "knowledgeAsk",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                GetKnowledgePartition(httpContext, includeUser: true),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = builder.Configuration.GetValue(
                        "RateLimiting:KnowledgeAskPermitLimit",
                        30),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    options.AddPolicy(
        "knowledgeUpload",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                GetKnowledgePartition(httpContext, includeUser: false),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = builder.Configuration.GetValue(
                        "RateLimiting:KnowledgeUploadPermitLimit",
                        10),
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.ActiveUser,
        UserCapability.AccessSystem);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.ManageUsers,
        UserCapability.ManageUsers);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.CustomerWrite,
        UserCapability.ManageCustomers);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.ServiceWrite,
        UserCapability.ManageServices);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.AppointmentManage,
        UserCapability.ManageAppointments);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.DashboardView,
        UserCapability.ViewDashboard);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.ReminderRetry,
        UserCapability.RetryReminders);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.KnowledgeManage,
        UserCapability.ManageKnowledgeDocuments);
    AddCapabilityPolicy(
        options,
        AuthorizationPolicies.KnowledgeUse,
        UserCapability.UseKnowledgeAssistant);
});
builder.Services.AddScoped<
    IAuthorizationHandler,
    UserCapabilityAuthorizationHandler>();
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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string? userIdValue = context.Principal?.FindFirst(
                    JwtRegisteredClaimNames.Sub)?.Value;
                string? organizationIdValue = context.Principal?.FindFirst(
                    AuthenticationClaimNames.OrganizationId)?.Value;
                string? versionValue = context.Principal?.FindFirst(
                    AuthenticationClaimNames.SessionVersion)?.Value;
                if (!Guid.TryParse(userIdValue, out Guid userId)
                    || !Guid.TryParse(organizationIdValue, out Guid organizationId)
                    || !int.TryParse(versionValue, out int sessionVersion))
                {
                    context.Fail("Session is invalid");
                    return;
                }

                ISessionVersionValidator validator = context.HttpContext.RequestServices
                    .GetRequiredService<ISessionVersionValidator>();
                bool isValid = await validator.IsValidAsync(
                    organizationId,
                    userId,
                    sessionVersion,
                    context.HttpContext.RequestAborted);
                if (!isValid)
                {
                    context.Fail("Session is no longer valid");
                }
            }
        };
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseForwardedHeaders();

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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    HttpContext httpContext =
        statusCodeContext.HttpContext;
    (string Code, string Detail) problem =
        httpContext.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized =>
                (
                    ApiProblemCodes
                        .AuthenticationRequired,
                    "Authentication is required."),
            StatusCodes.Status403Forbidden =>
                (
                    ApiProblemCodes
                        .AuthorizationForbidden,
                    "The operation is not allowed."),
            StatusCodes.Status404NotFound =>
                (
                    ApiProblemCodes.HttpNotFound,
                    "The requested resource was not found."),
            _ =>
                (
                    ApiProblemCodes.Unexpected,
                    "The request could not be completed.")
        };

    ApiProblemDetailsFactory factory =
        httpContext.RequestServices.GetRequiredService<
            ApiProblemDetailsFactory>();
    await factory.WriteAsync(
        httpContext,
        problem.Code,
        httpContext.Response.StatusCode,
        problem.Detail,
        httpContext.RequestAborted);
});
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
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet(
        "/",
        () => Results.Ok(new
        {
            service = "ServicePilot.Api",
            status = "running",
            health = new
            {
                live = "/health/live",
                ready = "/health/ready",
                knowledge = "/health/knowledge"
            }
        }))
    .AllowAnonymous()
    .ExcludeFromDescription();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready")
    });
app.MapHealthChecks(
    "/health/knowledge",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("knowledge")
    });

app.Run();

static string GetClientPartition(
    HttpContext httpContext)
{
    return httpContext.Connection.RemoteIpAddress?
        .ToString()
        ?? "unknown";
}

static string GetKnowledgePartition(
    HttpContext httpContext,
    bool includeUser)
{
    string? organizationId = httpContext.User.FindFirst(
        AuthenticationClaimNames.OrganizationId)?.Value;
    string? userId = httpContext.User.FindFirst(
        JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(organizationId))
    {
        return $"anonymous:{GetClientPartition(httpContext)}";
    }

    return includeUser && !string.IsNullOrWhiteSpace(userId)
        ? $"organization:{organizationId}:user:{userId}"
        : $"organization:{organizationId}";
}

static void AddCapabilityPolicy(
    AuthorizationOptions options,
    string policyName,
    UserCapability capability)
{
    options.AddPolicy(
        policyName,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new UserCapabilityRequirement(capability));
        });
}

public partial class Program;
